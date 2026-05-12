using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace SoulReaverEditor
{
    internal sealed class LevelProbe
    {
        public string Name;
        public int DataStart;
        public uint Version;
        public uint IntroCount;
        public uint VertexCount;
        public uint PolygonCount;
    }

    internal sealed class SR1LevelDocument
    {
        public BigFileEntry SourceEntry;
        public byte[] RawEntryBytes;
        public string Name;
        public int DataStart;
        public uint Version;
        public uint ModelData;
        public uint TerrainVertexStart;
        public uint TerrainPolygonStart;
        public uint MaterialStart;
        public readonly List<LevelObjectPlacement> Objects = new List<LevelObjectPlacement>();
        public readonly List<LevelPortal> Portals = new List<LevelPortal>();
        public readonly List<LevelVertex> Vertices = new List<LevelVertex>();
        public readonly List<LevelTriangle> Triangles = new List<LevelTriangle>();
        public RectangleF Bounds = RectangleF.Empty;

        public string Summary
        {
            get
            {
                return string.Format("{0}: {1} objects, {2} portals, {3} vertices, {4} faces",
                    string.IsNullOrEmpty(Name) ? "(unnamed room)" : Name,
                    Objects.Count,
                    Portals.Count,
                    Vertices.Count,
                    Triangles.Count);
            }
        }
    }

    internal sealed class LevelObjectPlacement
    {
        public int Index;
        public string Name;
        public string FileName;
        public int IntroNum;
        public int UniqueId;
        public int ModelIndex;
        public int MonsterAge;
        public short RotationRawX;
        public short RotationRawY;
        public short RotationRawZ;
        public short X;
        public short Y;
        public short Z;
        public short SpectralX;
        public short SpectralY;
        public short SpectralZ;
        public short OriginalRotationRawX;
        public short OriginalRotationRawY;
        public short OriginalRotationRawZ;
        public short OriginalX;
        public short OriginalY;
        public short OriginalZ;
        public short OriginalSpectralX;
        public short OriginalSpectralY;
        public short OriginalSpectralZ;
        public short MaxRad;
        public int EntryOffset;

        public float RotationDegreesX { get { return RotationRawX * 360.0f / 4096.0f; } }
        public float RotationDegreesY { get { return RotationRawY * 360.0f / 4096.0f; } }
        public float RotationDegreesZ { get { return RotationRawZ * 360.0f / 4096.0f; } }
        public bool HasSpectralPosition { get { return SpectralX != 0 || SpectralY != 0 || SpectralZ != 0; } }
        public bool HasMoved { get { return X != OriginalX || Y != OriginalY || Z != OriginalZ; } }
        public bool HasRotated { get { return RotationRawX != OriginalRotationRawX || RotationRawY != OriginalRotationRawY || RotationRawZ != OriginalRotationRawZ; } }
        public bool HasSpectralChanged { get { return SpectralX != OriginalSpectralX || SpectralY != OriginalSpectralY || SpectralZ != OriginalSpectralZ; } }
        public bool HasChanged { get { return HasMoved || HasRotated || HasSpectralChanged; } }

        public void ResetToOriginal()
        {
            RotationRawX = OriginalRotationRawX;
            RotationRawY = OriginalRotationRawY;
            RotationRawZ = OriginalRotationRawZ;
            X = OriginalX;
            Y = OriginalY;
            Z = OriginalZ;
            SpectralX = OriginalSpectralX;
            SpectralY = OriginalSpectralY;
            SpectralZ = OriginalSpectralZ;
        }
    }

    internal sealed class LevelPortal
    {
        public int Index;
        public string ToLevelName;
        public int SignalId;
        public short MinX, MinY, MinZ;
        public short MaxX, MaxY, MaxZ;
    }

    internal sealed class LevelVertex
    {
        public short X, Y, Z;
        public uint Color;
    }

    internal sealed class LevelTriangle
    {
        public int Index;
        public ushort A, B, C;
        public byte Attribute;
        public byte SortPush;
        public ushort Normal;
        public ushort TextureOffset;
    }

    internal static class SR1LevelParser
    {
        private const uint RetailVersion = 0x3C20413B;
        private const uint Beta19990512Version = 0x3C204139;

        public static bool TryProbe(Stream stream, BigFileEntry entry, out LevelProbe probe)
        {
            probe = null;
            try
            {
                byte[] head = ReadAt(stream, 0, (int)Math.Min(16, stream.Length));
                if (head.Length < 8) return false;
                uint first = Util.ReadUInt32LE(head, 0);
                uint second = Util.ReadUInt32LE(head, 4);
                if (second != 0) return false;

                int dataStart = (int)(((first >> 9) << 11) + 0x800);
                if (dataStart < 0 || dataStart + 0x100 >= stream.Length) return false;

                byte[] data = ReadAt(stream, dataStart, (int)Math.Min(0x100, stream.Length - dataStart));
                uint version = 0;
                if (data.Length > 0xF4) version = Util.ReadUInt32LE(data, 0xF0);
                if (version != RetailVersion && version != Beta19990512Version) return false;

                string name = "";
                uint nameOffset = Util.ReadUInt32LE(data, 0x98);
                if (nameOffset + 8 < stream.Length - dataStart)
                {
                    byte[] nameBytes = ReadAt(stream, dataStart + nameOffset, 8);
                    name = CleanName(Util.SafeAscii(nameBytes, 0, nameBytes.Length));
                }

                uint introCount = data.Length > 0x80 ? Util.ReadUInt32LE(data, 0x78) : 0;
                uint modelData = Util.ReadUInt32LE(data, 0);
                uint vertexCount = 0;
                uint polygonCount = 0;
                if (modelData + 0x18 < stream.Length - dataStart)
                {
                    byte[] modelHeader = ReadAt(stream, dataStart + modelData + 0x10, 8);
                    if (modelHeader.Length == 8)
                    {
                        vertexCount = Util.ReadUInt32LE(modelHeader, 0);
                        polygonCount = Util.ReadUInt32LE(modelHeader, 4);
                    }
                }

                if (introCount > 4096 || vertexCount > 100000 || polygonCount > 100000) return false;

                probe = new LevelProbe
                {
                    Name = name,
                    DataStart = dataStart,
                    Version = version,
                    IntroCount = introCount,
                    VertexCount = vertexCount,
                    PolygonCount = polygonCount
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryParse(Stream stream, BigFileEntry entry, out SR1LevelDocument document, out string error)
        {
            document = null;
            error = null;
            try
            {
                byte[] raw = Util.ReadUpTo(stream, checked((int)stream.Length));
                if (raw.Length < 0x100)
                {
                    error = "Entry is too small to be a Soul Reaver room.";
                    return false;
                }

                uint first = Util.ReadUInt32LE(raw, 0);
                uint second = Util.ReadUInt32LE(raw, 4);
                if (second != 0)
                {
                    error = "Entry is not a room/unit resource.";
                    return false;
                }

                int dataStart = (int)(((first >> 9) << 11) + 0x800);
                if (dataStart < 0 || dataStart + 0x100 >= raw.Length)
                {
                    error = "Room data pointer points outside the entry.";
                    return false;
                }

                byte[] data = new byte[raw.Length - dataStart];
                Array.Copy(raw, dataStart, data, 0, data.Length);

                uint version = ReadU32(data, 0xF0);
                if (version != RetailVersion && version != Beta19990512Version)
                {
                    error = "Only the retail/Beta 1999-05-12 room layout is mapped in this editor tab so far.";
                    return false;
                }

                document = new SR1LevelDocument();
                document.SourceEntry = entry;
                document.RawEntryBytes = raw;
                document.DataStart = dataStart;
                document.Version = version;
                document.ModelData = ReadU32(data, 0);

                uint nameOffset = ReadU32(data, 0x98);
                document.Name = nameOffset + 8 < data.Length ? CleanName(ReadAscii(data, (int)nameOffset, 8)) : entry.VirtualPath;

                ParseTerrain(data, document);
                ParsePortals(data, document);
                ParseObjects(data, document);
                ComputeBounds(document);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                document = null;
                return false;
            }
        }

        public static void WriteObjectToRaw(SR1LevelDocument document, LevelObjectPlacement obj)
        {
            if (document == null || document.RawEntryBytes == null || obj == null) return;
            WriteObjectToBytes(document.RawEntryBytes, document.DataStart, obj, false);
        }

        public static void WriteObjectToBytes(byte[] rawEntryBytes, int dataStart, LevelObjectPlacement obj, bool originalValues)
        {
            if (rawEntryBytes == null || obj == null) return;
            int p = dataStart + obj.EntryOffset;
            if (p < 0 || p + 0x4A > rawEntryBytes.Length) return;

            WriteInt16(rawEntryBytes, p + 0x18, originalValues ? obj.OriginalRotationRawX : obj.RotationRawX);
            WriteInt16(rawEntryBytes, p + 0x1A, originalValues ? obj.OriginalRotationRawY : obj.RotationRawY);
            WriteInt16(rawEntryBytes, p + 0x1C, originalValues ? obj.OriginalRotationRawZ : obj.RotationRawZ);
            WriteInt16(rawEntryBytes, p + 0x20, originalValues ? obj.OriginalX : obj.X);
            WriteInt16(rawEntryBytes, p + 0x22, originalValues ? obj.OriginalY : obj.Y);
            WriteInt16(rawEntryBytes, p + 0x24, originalValues ? obj.OriginalZ : obj.Z);
            WriteInt16(rawEntryBytes, p + 0x44, originalValues ? obj.OriginalSpectralX : obj.SpectralX);
            WriteInt16(rawEntryBytes, p + 0x46, originalValues ? obj.OriginalSpectralY : obj.SpectralY);
            WriteInt16(rawEntryBytes, p + 0x48, originalValues ? obj.OriginalSpectralZ : obj.SpectralZ);
        }

        private static void ParseTerrain(byte[] data, SR1LevelDocument document)
        {
            uint modelData = document.ModelData;
            if (modelData == 0 || modelData + 0x38 >= data.Length) return;

            int p = (int)modelData + 0x10;
            uint vertexCount = ReadU32(data, p);
            uint polygonCount = ReadU32(data, p + 4);
            p += 12;
            uint vertexStart = ReadU32(data, p);
            uint polygonStart = ReadU32(data, p + 4);
            p += 24;
            uint materialStart = ReadU32(data, p);

            document.TerrainVertexStart = vertexStart;
            document.TerrainPolygonStart = polygonStart;
            document.MaterialStart = materialStart;

            if (vertexCount < 100000 && vertexStart + vertexCount * 12 <= data.Length)
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    int v = (int)vertexStart + i * 12;
                    LevelVertex vertex = new LevelVertex();
                    vertex.X = ReadI16(data, v);
                    vertex.Y = ReadI16(data, v + 2);
                    vertex.Z = ReadI16(data, v + 4);
                    vertex.Color = ReadU32(data, v + 8) | 0xFF000000;
                    document.Vertices.Add(vertex);
                }
            }

            if (polygonCount < 100000 && polygonStart + polygonCount * 12 <= data.Length)
            {
                for (int i = 0; i < polygonCount; i++)
                {
                    int f = (int)polygonStart + i * 12;
                    LevelTriangle tri = new LevelTriangle();
                    tri.Index = i;
                    tri.A = ReadU16(data, f);
                    tri.B = ReadU16(data, f + 2);
                    tri.C = ReadU16(data, f + 4);
                    tri.Attribute = data[f + 6];
                    tri.SortPush = data[f + 7];
                    tri.Normal = ReadU16(data, f + 8);
                    tri.TextureOffset = ReadU16(data, f + 10);
                    if (tri.A < document.Vertices.Count && tri.B < document.Vertices.Count && tri.C < document.Vertices.Count)
                    {
                        document.Triangles.Add(tri);
                    }
                }
            }
        }

        private static void ParsePortals(byte[] data, SR1LevelDocument document)
        {
            uint terrainData = ReadU32(data, 0);
            if (terrainData + 0x34 >= data.Length) return;
            uint portalStartPointer = ReadU32(data, (int)terrainData + 0x30);
            if (portalStartPointer == 0 || portalStartPointer + 4 >= data.Length) return;
            uint portalCount = ReadU32(data, (int)portalStartPointer);
            if (portalCount > 512) return;

            int p = (int)portalStartPointer + 4;
            for (int i = 0; i < portalCount; i++)
            {
                if (p + 92 > data.Length) break;
                LevelPortal portal = new LevelPortal();
                portal.Index = i;
                portal.ToLevelName = CleanName(ReadAscii(data, p, 16));
                p += 16;
                portal.SignalId = ReadI32(data, p);
                p += 4;
                p += 4;
                portal.MinX = ReadI16(data, p);
                portal.MinY = ReadI16(data, p + 2);
                portal.MinZ = ReadI16(data, p + 4);
                p += 8;
                portal.MaxX = ReadI16(data, p);
                portal.MaxY = ReadI16(data, p + 2);
                portal.MaxZ = ReadI16(data, p + 4);
                p += 8;
                p += 4;
                p += 48;
                document.Portals.Add(portal);
            }
        }

        private static void ParseObjects(byte[] data, SR1LevelDocument document)
        {
            uint introCount = ReadU32(data, 0x78);
            uint introStart = ReadU32(data, 0x7C);
            if (introCount > 4096 || introStart + introCount * 0x4C > data.Length) return;

            for (int i = 0; i < introCount; i++)
            {
                int p = (int)introStart + i * 0x4C;
                LevelObjectPlacement obj = new LevelObjectPlacement();
                obj.Index = i;
                obj.EntryOffset = p;
                obj.FileName = CleanObjectName(ReadAscii(data, p, 16));
                obj.IntroNum = ReadI32(data, p + 0x10);
                obj.UniqueId = ReadI32(data, p + 0x14);
                obj.RotationRawX = ReadI16(data, p + 0x18);
                obj.RotationRawY = ReadI16(data, p + 0x1A);
                obj.RotationRawZ = ReadI16(data, p + 0x1C);
                obj.X = ReadI16(data, p + 0x20);
                obj.Y = ReadI16(data, p + 0x22);
                obj.Z = ReadI16(data, p + 0x24);
                obj.MaxRad = ReadI16(data, p + 0x26);
                obj.SpectralX = ReadI16(data, p + 0x44);
                obj.SpectralY = ReadI16(data, p + 0x46);
                obj.SpectralZ = ReadI16(data, p + 0x48);
                obj.OriginalRotationRawX = obj.RotationRawX;
                obj.OriginalRotationRawY = obj.RotationRawY;
                obj.OriginalRotationRawZ = obj.RotationRawZ;
                obj.OriginalX = obj.X;
                obj.OriginalY = obj.Y;
                obj.OriginalZ = obj.Z;
                obj.OriginalSpectralX = obj.SpectralX;
                obj.OriginalSpectralY = obj.SpectralY;
                obj.OriginalSpectralZ = obj.SpectralZ;
                obj.Name = obj.FileName + "-" + obj.UniqueId;

                uint iniCommand = ReadU32(data, p + 0x30);
                ParseIntroCommands(data, obj, iniCommand);
                document.Objects.Add(obj);
            }
        }

        private static void ParseIntroCommands(byte[] data, LevelObjectPlacement obj, uint iniCommand)
        {
            int guard = 0;
            uint p = iniCommand;
            while (p != 0 && p + 4 <= data.Length && guard++ < 128)
            {
                ushort command = ReadU16(data, (int)p);
                if (command == 0) break;
                ushort numParameters = ReadU16(data, (int)p + 2);
                if (p + 4 + numParameters * 4 > data.Length) break;
                if (command == 6 && numParameters >= 1)
                {
                    obj.MonsterAge = ReadI32(data, (int)p + 4);
                }
                else if (command == 18 && numParameters >= 1)
                {
                    obj.ModelIndex = ReadI32(data, (int)p + 4);
                }
                p += 4u + 4u * numParameters;
            }
        }

        private static void ComputeBounds(SR1LevelDocument document)
        {
            bool any = false;
            float minX = 0, minZ = 0, maxX = 0, maxZ = 0;
            Action<float, float> include = delegate(float x, float z)
            {
                if (!any)
                {
                    minX = maxX = x;
                    minZ = maxZ = z;
                    any = true;
                }
                else
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (z < minZ) minZ = z;
                    if (z > maxZ) maxZ = z;
                }
            };

            foreach (LevelVertex vertex in document.Vertices) include(vertex.X, vertex.Z);
            foreach (LevelObjectPlacement obj in document.Objects) include(obj.X, obj.Z);
            foreach (LevelPortal portal in document.Portals)
            {
                include(portal.MinX, portal.MinZ);
                include(portal.MaxX, portal.MaxZ);
            }

            if (!any)
            {
                document.Bounds = new RectangleF(-1024, -1024, 2048, 2048);
            }
            else
            {
                if (maxX - minX < 32) maxX = minX + 32;
                if (maxZ - minZ < 32) maxZ = minZ + 32;
                document.Bounds = RectangleF.FromLTRB(minX, minZ, maxX, maxZ);
            }
        }

        private static byte[] ReadAt(Stream stream, long offset, int count)
        {
            long old = stream.CanSeek ? stream.Position : 0;
            try
            {
                stream.Position = offset;
                return Util.ReadUpTo(stream, count);
            }
            finally
            {
                if (stream.CanSeek) stream.Position = old;
            }
        }

        private static string ReadAscii(byte[] data, int offset, int count)
        {
            if (offset < 0 || offset + count > data.Length) return "";
            return Encoding.ASCII.GetString(data, offset, count).TrimEnd('\0', ' ');
        }

        private static string CleanName(string name)
        {
            if (name == null) return "";
            int zero = name.IndexOf('\0');
            if (zero >= 0) name = name.Substring(0, zero);
            return name.Trim().ToLowerInvariant();
        }

        private static string CleanObjectName(string name)
        {
            return CleanName(name).TrimEnd('_');
        }

        private static ushort ReadU16(byte[] data, int offset)
        {
            return Util.ReadUInt16LE(data, offset);
        }

        private static short ReadI16(byte[] data, int offset)
        {
            return Util.ReadInt16LE(data, offset);
        }

        private static int ReadI32(byte[] data, int offset)
        {
            return unchecked((int)Util.ReadUInt32LE(data, offset));
        }

        private static uint ReadU32(byte[] data, int offset)
        {
            return Util.ReadUInt32LE(data, offset);
        }

        private static void WriteInt16(byte[] data, int offset, short value)
        {
            data[offset] = (byte)(value & 0xFF);
            data[offset + 1] = (byte)((value >> 8) & 0xFF);
        }
    }
}
