using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SoulReaverEditor
{
    internal static class Util
    {
        public static ushort ReadUInt16LE(byte[] data, int offset)
        {
            return (ushort)(data[offset] | (data[offset + 1] << 8));
        }

        public static short ReadInt16LE(byte[] data, int offset)
        {
            return unchecked((short)ReadUInt16LE(data, offset));
        }

        public static uint ReadUInt32LE(byte[] data, int offset)
        {
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }

        public static uint ReadUInt32LE(Stream stream)
        {
            int b0 = stream.ReadByte();
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            int b3 = stream.ReadByte();
            if ((b0 | b1 | b2 | b3) < 0) throw new EndOfStreamException();
            return (uint)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));
        }

        public static ushort ReadUInt16Xor(byte[] data, int offset, ushort key)
        {
            return (ushort)(ReadUInt16LE(data, offset) ^ key);
        }

        public static uint ReadUInt32Xor(byte[] data, int offset, ushort key)
        {
            uint lo = (uint)ReadUInt16Xor(data, offset, key);
            uint hi = (uint)ReadUInt16Xor(data, offset + 2, key);
            return lo | (hi << 16);
        }

        public static string SafeAscii(byte[] data, int offset, int count)
        {
            if (count <= 0) return string.Empty;
            return Encoding.ASCII.GetString(data, offset, count).TrimEnd('\0', ' ');
        }

        public static string FormatSize(long size)
        {
            string[] suffix = { "B", "KB", "MB", "GB" };
            double value = size;
            int i = 0;
            while (value >= 1024.0 && i < suffix.Length - 1)
            {
                value /= 1024.0;
                i++;
            }
            if (i == 0) return size + " B";
            return value.ToString("0.##") + " " + suffix[i];
        }

        public static string HexDump(byte[] data, int length)
        {
            StringBuilder sb = new StringBuilder();
            int rows = Math.Min(length, data.Length);
            for (int offset = 0; offset < rows; offset += 16)
            {
                int take = Math.Min(16, rows - offset);
                sb.Append(offset.ToString("X8"));
                sb.Append("  ");
                for (int i = 0; i < 16; i++)
                {
                    if (i < take) sb.Append(data[offset + i].ToString("X2"));
                    else sb.Append("  ");
                    sb.Append(i == 7 ? "  " : " ");
                }
                sb.Append(" ");
                for (int i = 0; i < take; i++)
                {
                    byte b = data[offset + i];
                    sb.Append(b >= 32 && b < 127 ? (char)b : '.');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static List<string> ExtractStrings(Stream stream, int maxBytes, int minLength)
        {
            List<string> results = new List<string>();
            long oldPos = stream.CanSeek ? stream.Position : 0;
            try
            {
                if (stream.CanSeek) stream.Position = 0;
                int remaining = maxBytes;
                StringBuilder sb = new StringBuilder();
                while (remaining > 0)
                {
                    int b = stream.ReadByte();
                    if (b < 0) break;
                    remaining--;
                    if (b >= 32 && b < 127)
                    {
                        sb.Append((char)b);
                    }
                    else
                    {
                        if (sb.Length >= minLength) results.Add(sb.ToString());
                        sb.Length = 0;
                        if (results.Count >= 500) break;
                    }
                }
                if (sb.Length >= minLength && results.Count < 500) results.Add(sb.ToString());
            }
            finally
            {
                if (stream.CanSeek) stream.Position = oldPos;
            }
            return results;
        }

        public static byte[] ReadUpTo(Stream stream, int count)
        {
            byte[] data = new byte[count];
            int total = 0;
            while (total < count)
            {
                int read = stream.Read(data, total, count - total);
                if (read <= 0) break;
                total += read;
            }
            if (total == data.Length) return data;
            byte[] trimmed = new byte[total];
            Array.Copy(data, trimmed, total);
            return trimmed;
        }

        public static byte[] ParseHexPattern(string text)
        {
            string clean = text.Replace(" ", "").Replace("-", "").Replace(",", "").Replace("0x", "").Replace("0X", "");
            if (clean.Length == 0 || (clean.Length % 2) != 0)
            {
                throw new FormatException("Hex searches need an even number of hex digits.");
            }
            byte[] bytes = new byte[clean.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(clean.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        public static string MakeSafeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                bool bad = false;
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (ch == invalid[j])
                    {
                        bad = true;
                        break;
                    }
                }
                sb.Append(bad ? '_' : ch);
            }
            return sb.ToString();
        }
    }
}
