using System;
using System.Collections.Generic;

namespace SoulReaverEditor
{
    internal static class Iso9660Reader
    {
        public static IsoFileEntry Read(DiscImage image)
        {
            byte[] pvd = image.ReadSector(16);
            string id = Util.SafeAscii(pvd, 1, 5);
            if (pvd[0] != 1 || id != "CD001")
            {
                throw new InvalidOperationException("The image does not contain an ISO9660 primary volume descriptor at sector 16.");
            }

            IsoFileEntry root = ReadDirectoryRecord(pvd, 156, "");
            root.Name = "/";
            root.FullPath = "/";
            HashSet<uint> visited = new HashSet<uint>();
            ReadDirectory(image, root, visited, 0);
            return root;
        }

        private static IsoFileEntry ReadDirectoryRecord(byte[] data, int offset, string parentPath)
        {
            byte length = data[offset];
            if (length == 0) return null;

            uint lba = Util.ReadUInt32LE(data, offset + 2);
            uint size = Util.ReadUInt32LE(data, offset + 10);
            byte flags = data[offset + 25];
            byte nameLength = data[offset + 32];
            string name;
            if (nameLength == 1 && data[offset + 33] == 0) name = ".";
            else if (nameLength == 1 && data[offset + 33] == 1) name = "..";
            else
            {
                name = Util.SafeAscii(data, offset + 33, nameLength);
                int semi = name.IndexOf(';');
                if (semi >= 0) name = name.Substring(0, semi);
            }

            string fullPath;
            if (parentPath == "/" || string.IsNullOrEmpty(parentPath)) fullPath = "/" + name;
            else fullPath = parentPath + "/" + name;

            return new IsoFileEntry
            {
                Name = name,
                FullPath = fullPath,
                IsDirectory = (flags & 2) != 0,
                Lba = lba,
                Size = size
            };
        }

        private static void ReadDirectory(DiscImage image, IsoFileEntry dir, HashSet<uint> visited, int depth)
        {
            if (depth > 32) return;
            if (visited.Contains(dir.Lba)) return;
            visited.Add(dir.Lba);

            byte[] data = new byte[dir.Size];
            int copied = 0;
            uint sectorCount = (dir.Size + DiscImage.UserSectorSize - 1) / DiscImage.UserSectorSize;
            for (uint i = 0; i < sectorCount; i++)
            {
                byte[] sector = image.ReadSector(dir.Lba + i);
                int take = Math.Min(DiscImage.UserSectorSize, data.Length - copied);
                Array.Copy(sector, 0, data, copied, take);
                copied += take;
            }

            int pos = 0;
            while (pos < data.Length)
            {
                int len = data[pos];
                if (len == 0)
                {
                    pos = ((pos / DiscImage.UserSectorSize) + 1) * DiscImage.UserSectorSize;
                    continue;
                }

                IsoFileEntry entry = ReadDirectoryRecord(data, pos, dir.FullPath);
                if (entry != null && entry.Name != "." && entry.Name != "..")
                {
                    dir.Children.Add(entry);
                    if (entry.IsDirectory)
                    {
                        ReadDirectory(image, entry, visited, depth + 1);
                    }
                }
                pos += len;
            }
        }
    }
}
