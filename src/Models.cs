using System;
using System.Collections.Generic;

namespace SoulReaverEditor
{
    internal sealed class IsoFileEntry
    {
        public string Name;
        public string FullPath;
        public bool IsDirectory;
        public uint Lba;
        public uint Size;
        public readonly List<IsoFileEntry> Children = new List<IsoFileEntry>();

        public override string ToString()
        {
            return Name;
        }
    }

    internal sealed class BigFileFolder
    {
        public int Index;
        public short Unknown;
        public ushort FileCount;
        public uint Offset;
        public ushort XorKey;
        public readonly List<BigFileEntry> Files = new List<BigFileEntry>();

        public string DisplayName
        {
            get { return string.Format("Folder {0:X4} ({1} files)", Index, Files.Count); }
        }
    }

    internal sealed class BigFileEntry
    {
        public int FolderIndex;
        public int FileIndex;
        public uint Hash1;
        public uint Hash2;
        public uint Size;
        public uint Offset;
        public string Kind;
        public string Notes;

        public string VirtualPath
        {
            get { return string.Format("/BIGFILE.DAT/folder{0:X4}/file{1:X4}", FolderIndex, FileIndex); }
        }

        public string DisplayName
        {
            get { return string.Format("file{0:X4}  {1}", FileIndex, Kind ?? "Unknown"); }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    internal sealed class SignatureHit
    {
        public long Offset;
        public string Kind;
        public string Detail;
    }
}
