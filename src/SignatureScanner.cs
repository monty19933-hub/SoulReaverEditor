using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SoulReaverEditor
{
    internal static class SignatureScanner
    {
        public static string ClassifyHeader(byte[] head, uint size, out string notes)
        {
            notes = "";
            if (head == null || head.Length == 0) return "Empty";

            string ascii4 = head.Length >= 4 ? Encoding.ASCII.GetString(head, 0, 4) : "";
            if (ascii4 == "VAGp") return "VAG audio";
            if (ascii4 == "SEQp") return "SEQ music";
            if (ascii4 == "DNSa") return "DNSa sequence";
            if (ascii4 == "PMSa") return "PMSa samples";
            if (ascii4 == "FNSa") return "FNSa sound";
            if (ascii4 == "FMSa") return "FMSa samples";
            if (ascii4 == "RIFF") return "RIFF/WAVE";
            if (head.Length >= 8 && Encoding.ASCII.GetString(head, 0, 8) == "PS-X EXE") return "PS-X EXE";

            if (LooksLikeTim(head))
            {
                notes = DescribeTim(head);
                return "TIM texture/palette";
            }

            if (LooksLikeMalformedSoulReaverTim(head))
            {
                notes = "Soul Reaver/Gex-style malformed TIM header.";
                return "TIM-like texture";
            }

            if (head.Length >= 4 && head[0] == 0x00 && head[1] == 0x00 && head[2] == 0x01 && head[3] == 0xBA)
            {
                return "MPEG stream";
            }

            int printable = 0;
            for (int i = 0; i < head.Length; i++)
            {
                if (head[i] >= 32 && head[i] < 127) printable++;
            }
            if (head.Length > 0 && printable > head.Length * 3 / 4) return "Text/table";

            if (size == 24 || size == 28 || size == 32 || size == 48) return "Tiny metadata";
            if (size >= 200000 && size <= 270000) return "Likely room/texture block";
            return "Unknown";
        }

        private static bool LooksLikeTim(byte[] head)
        {
            if (head.Length < 8) return false;
            if (Util.ReadUInt32LE(head, 0) != 0x10) return false;
            uint flags = Util.ReadUInt32LE(head, 4);
            return flags <= 0x0F || (flags & 0x08) != 0;
        }

        private static string DescribeTim(byte[] head)
        {
            if (head.Length < 8) return "";
            uint flags = Util.ReadUInt32LE(head, 4);
            int bpp = (int)(flags & 7);
            string depth = bpp == 0 ? "4bpp" : bpp == 1 ? "8bpp" : bpp == 2 ? "16bpp" : bpp == 3 ? "24bpp" : "unknown bpp";
            bool hasClut = (flags & 8) != 0;
            return depth + (hasClut ? ", CLUT present" : ", no CLUT flag");
        }

        private static bool LooksLikeMalformedSoulReaverTim(byte[] head)
        {
            if (head.Length < 20) return false;
            byte[] a = { 0x10, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x04, 0x00, 0x08, 0x00 };
            for (int i = 0; i < a.Length; i++)
            {
                if (head[i] != a[i]) return false;
            }
            return true;
        }

        public static List<SignatureHit> Scan(Stream stream, int maxBytes)
        {
            List<SignatureHit> hits = new List<SignatureHit>();
            long old = stream.CanSeek ? stream.Position : 0;
            try
            {
                if (stream.CanSeek) stream.Position = 0;
                byte[] data = Util.ReadUpTo(stream, maxBytes);
                for (int i = 0; i <= data.Length - 4; i++)
                {
                    if (i <= data.Length - 8 && Util.ReadUInt32LE(data, i) == 0x10)
                    {
                        byte[] head = Slice(data, i, Math.Min(64, data.Length - i));
                        if (LooksLikeTim(head))
                        {
                            string detail = DescribeTim(head);
                            hits.Add(new SignatureHit { Offset = i, Kind = "TIM", Detail = detail });
                        }
                    }

                    string magic = Encoding.ASCII.GetString(data, i, 4);
                    if (magic == "VAGp" || magic == "SEQp" || magic == "DNSa" || magic == "PMSa" || magic == "FNSa" || magic == "FMSa")
                    {
                        hits.Add(new SignatureHit { Offset = i, Kind = magic, Detail = "PlayStation/Soul Reaver audio signature" });
                    }
                }
            }
            finally
            {
                if (stream.CanSeek) stream.Position = old;
            }
            return hits;
        }

        private static byte[] Slice(byte[] data, int offset, int count)
        {
            byte[] result = new byte[count];
            Array.Copy(data, offset, result, 0, count);
            return result;
        }
    }
}
