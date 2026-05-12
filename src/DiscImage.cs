using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SoulReaverEditor
{
    internal sealed class DiscImage : IDisposable
    {
        public const int UserSectorSize = 2048;

        private readonly FileStream _stream;

        public string ImagePath { get; private set; }
        public int RawSectorSize { get; private set; }
        public int UserDataOffset { get; private set; }
        public bool IsRawImage { get; private set; }

        private DiscImage(string imagePath, int rawSectorSize, int userDataOffset, bool isRaw)
        {
            ImagePath = imagePath;
            RawSectorSize = rawSectorSize;
            UserDataOffset = userDataOffset;
            IsRawImage = isRaw;
            _stream = File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public static DiscImage Open(string path)
        {
            string imagePath = path;
            if (Path.GetExtension(path).Equals(".cue", StringComparison.OrdinalIgnoreCase))
            {
                imagePath = ResolveCueBinary(path);
            }

            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Disc image not found.", imagePath);
            }

            Detection detection = DetectImageLayout(imagePath);
            return new DiscImage(imagePath, detection.RawSectorSize, detection.UserDataOffset, detection.IsRaw);
        }

        private static string ResolveCueBinary(string cuePath)
        {
            string cueDir = Path.GetDirectoryName(cuePath);
            foreach (string rawLine in File.ReadAllLines(cuePath))
            {
                string line = rawLine.Trim();
                if (!line.StartsWith("FILE ", StringComparison.OrdinalIgnoreCase)) continue;

                Match quoted = Regex.Match(line, "^FILE\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);
                string fileName = quoted.Success ? quoted.Groups[1].Value : null;
                if (fileName == null)
                {
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) fileName = parts[1];
                }

                if (!string.IsNullOrEmpty(fileName))
                {
                    string candidate = Path.IsPathRooted(fileName) ? fileName : Path.Combine(cueDir, fileName);
                    if (File.Exists(candidate)) return candidate;
                    return candidate;
                }
            }

            throw new InvalidDataException("The CUE file does not contain a FILE entry.");
        }

        private struct Detection
        {
            public int RawSectorSize;
            public int UserDataOffset;
            public bool IsRaw;
        }

        private static Detection DetectImageLayout(string path)
        {
            long length = new FileInfo(path).Length;
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (length >= 17L * 2352L && LooksLikePvd(fs, 2352, 24))
                {
                    return new Detection { RawSectorSize = 2352, UserDataOffset = 24, IsRaw = true };
                }
                if (length >= 17L * 2352L && LooksLikePvd(fs, 2352, 16))
                {
                    return new Detection { RawSectorSize = 2352, UserDataOffset = 16, IsRaw = true };
                }
                if (length >= 17L * 2048L && LooksLikePvd(fs, 2048, 0))
                {
                    return new Detection { RawSectorSize = 2048, UserDataOffset = 0, IsRaw = false };
                }
            }

            throw new InvalidDataException("Could not find an ISO9660 primary volume descriptor in this image.");
        }

        private static bool LooksLikePvd(FileStream fs, int sectorSize, int dataOffset)
        {
            byte[] id = new byte[5];
            long pos = 16L * sectorSize + dataOffset + 1;
            if (pos + id.Length > fs.Length) return false;
            fs.Position = pos;
            int read = fs.Read(id, 0, id.Length);
            if (read != id.Length) return false;
            return id[0] == (byte)'C' && id[1] == (byte)'D' && id[2] == (byte)'0' && id[3] == (byte)'0' && id[4] == (byte)'1';
        }

        public long LogicalSectorCount
        {
            get { return _stream.Length / RawSectorSize; }
        }

        public byte[] ReadSector(uint lba)
        {
            byte[] data = new byte[UserSectorSize];
            ReadSector(lba, data, 0);
            return data;
        }

        public void ReadSector(uint lba, byte[] buffer, int offset)
        {
            lock (_stream)
            {
                long pos = (long)lba * RawSectorSize + UserDataOffset;
                if (pos + UserSectorSize > _stream.Length)
                {
                    throw new EndOfStreamException("Sector extends past the end of the image.");
                }
                _stream.Position = pos;
                int total = 0;
                while (total < UserSectorSize)
                {
                    int read = _stream.Read(buffer, offset + total, UserSectorSize - total);
                    if (read <= 0) throw new EndOfStreamException();
                    total += read;
                }
            }
        }

        public int ReadFileBytes(IsoFileEntry entry, long fileOffset, byte[] buffer, int offset, int count)
        {
            if (fileOffset >= entry.Size) return 0;
            int allowed = (int)Math.Min(count, (long)entry.Size - fileOffset);
            int total = 0;
            while (total < allowed)
            {
                long logical = fileOffset + total;
                uint sector = entry.Lba + (uint)(logical / UserSectorSize);
                int inSector = (int)(logical % UserSectorSize);
                int take = Math.Min(allowed - total, UserSectorSize - inSector);

                lock (_stream)
                {
                    long pos = (long)sector * RawSectorSize + UserDataOffset + inSector;
                    _stream.Position = pos;
                    int got = _stream.Read(buffer, offset + total, take);
                    if (got <= 0) break;
                    total += got;
                    if (got < take) break;
                }
            }
            return total;
        }

        public Stream OpenFile(IsoFileEntry entry)
        {
            return new IsoFileStream(this, entry);
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }

    internal sealed class IsoFileStream : Stream
    {
        private readonly DiscImage _image;
        private readonly IsoFileEntry _entry;
        private long _position;

        public IsoFileStream(DiscImage image, IsoFileEntry entry)
        {
            _image = image;
            _entry = entry;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return _entry.Size; } }
        public override long Position
        {
            get { return _position; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _image.ReadFileBytes(_entry, _position, buffer, offset, count);
            _position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long next;
            if (origin == SeekOrigin.Begin) next = offset;
            else if (origin == SeekOrigin.Current) next = _position + offset;
            else next = Length + offset;

            if (next < 0) throw new IOException("Cannot seek before the start of the file.");
            _position = next;
            return _position;
        }

        public override void Flush() { }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    }

    internal sealed class SubStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _start;
        private readonly long _length;
        private readonly bool _leaveOpen;
        private long _position;

        public SubStream(Stream baseStream, long start, long length, bool leaveOpen)
        {
            _baseStream = baseStream;
            _start = start;
            _length = length;
            _leaveOpen = leaveOpen;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return _length; } }
        public override long Position
        {
            get { return _position; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length) return 0;
            int allowed = (int)Math.Min(count, _length - _position);
            lock (_baseStream)
            {
                _baseStream.Position = _start + _position;
                int read = _baseStream.Read(buffer, offset, allowed);
                _position += read;
                return read;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long next;
            if (origin == SeekOrigin.Begin) next = offset;
            else if (origin == SeekOrigin.Current) next = _position + offset;
            else next = _length + offset;
            if (next < 0) throw new IOException("Cannot seek before the start of the file.");
            _position = next;
            return _position;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveOpen) _baseStream.Dispose();
            base.Dispose(disposing);
        }

        public override void Flush() { }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    }
}
