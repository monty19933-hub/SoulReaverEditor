using System;
using System.Collections.Generic;
using System.IO;

namespace SoulReaverEditor
{
    internal sealed class BigFileReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly bool _leaveOpen;

        public readonly List<BigFileFolder> Folders = new List<BigFileFolder>();

        public BigFileReader(Stream stream, bool leaveOpen)
        {
            _stream = stream;
            _leaveOpen = leaveOpen;
            Parse();
        }

        private void Parse()
        {
            byte[] header = ReadAt(0, 4);
            ushort folderCount = Util.ReadUInt16LE(header, 0);
            if (folderCount == 0 || folderCount > 4096)
            {
                throw new InvalidDataException("BIGFILE.DAT has an unexpected folder count.");
            }

            byte[] folderTable = ReadAt(4, folderCount * 8);
            for (int i = 0; i < folderCount; i++)
            {
                int p = i * 8;
                BigFileFolder folder = new BigFileFolder();
                folder.Index = i;
                folder.Unknown = Util.ReadInt16LE(folderTable, p);
                folder.FileCount = Util.ReadUInt16LE(folderTable, p + 2);
                folder.Offset = Util.ReadUInt32LE(folderTable, p + 4);
                Folders.Add(folder);
            }

            foreach (BigFileFolder folder in Folders)
            {
                ParseFolder(folder);
            }
        }

        private void ParseFolder(BigFileFolder folder)
        {
            int tableSize = 4 + folder.FileCount * 16;
            byte[] data = ReadAt(folder.Offset, tableSize);
            ushort encryptedCount = Util.ReadUInt16LE(data, 0);
            ushort encryptedZero = Util.ReadUInt16LE(data, 2);
            ushort key = 0;
            if (encryptedCount != folder.FileCount || encryptedZero != 0)
            {
                key = (ushort)(encryptedCount ^ folder.FileCount);
            }
            folder.XorKey = key;

            ushort decodedCount = Util.ReadUInt16Xor(data, 0, key);
            if (decodedCount != folder.FileCount)
            {
                throw new InvalidDataException(string.Format("Folder {0:X4} has an invalid decoded file count.", folder.Index));
            }

            for (int i = 0; i < folder.FileCount; i++)
            {
                int p = 4 + i * 16;
                BigFileEntry entry = new BigFileEntry();
                entry.FolderIndex = folder.Index;
                entry.FileIndex = i;
                entry.Hash1 = Util.ReadUInt32Xor(data, p, key);
                entry.Size = Util.ReadUInt32Xor(data, p + 4, key);
                entry.Offset = Util.ReadUInt32Xor(data, p + 8, key);
                entry.Hash2 = Util.ReadUInt32Xor(data, p + 12, key);
                Classify(entry);
                folder.Files.Add(entry);
            }
        }

        private void Classify(BigFileEntry entry)
        {
            if (entry.Size == 0)
            {
                entry.Kind = "Empty";
                return;
            }

            byte[] head;
            try
            {
                head = ReadAt(entry.Offset, (int)Math.Min(512, entry.Size));
            }
            catch
            {
                entry.Kind = "Invalid";
                entry.Notes = "Entry points outside BIGFILE.DAT.";
                return;
            }

            entry.Kind = SignatureScanner.ClassifyHeader(head, entry.Size, out entry.Notes);
        }

        private byte[] ReadAt(long offset, int count)
        {
            byte[] data = new byte[count];
            lock (_stream)
            {
                _stream.Position = offset;
                int total = 0;
                while (total < count)
                {
                    int read = _stream.Read(data, total, count - total);
                    if (read <= 0) throw new EndOfStreamException();
                    total += read;
                }
            }
            return data;
        }

        public Stream OpenFile(BigFileEntry entry)
        {
            return new SubStream(_stream, entry.Offset, entry.Size, true);
        }

        public IEnumerable<BigFileEntry> AllFiles()
        {
            foreach (BigFileFolder folder in Folders)
            {
                foreach (BigFileEntry entry in folder.Files)
                {
                    yield return entry;
                }
            }
        }

        public void Dispose()
        {
            if (!_leaveOpen) _stream.Dispose();
        }
    }
}
