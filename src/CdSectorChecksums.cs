using System;

namespace SoulReaverEditor
{
    internal static class CdSectorChecksums
    {
        private static readonly uint[] EdcTable = BuildEdcTable();
        private static readonly byte[] EccForwardTable = new byte[256];
        private static readonly byte[] EccBackwardTable = new byte[256];

        static CdSectorChecksums()
        {
            for (int i = 0; i < 256; i++)
            {
                int j = (i << 1) ^ ((i & 0x80) != 0 ? 0x11D : 0);
                EccForwardTable[i] = (byte)j;
                EccBackwardTable[i ^ j] = (byte)i;
            }
        }

        public static void Recalculate(byte[] sector, int userDataOffset)
        {
            if (sector == null || sector.Length < 2352) return;

            byte mode = sector[0x0F];
            if (mode == 1 || userDataOffset == 16)
            {
                RecalculateMode1(sector);
            }
            else if (mode == 2 || userDataOffset == 24)
            {
                RecalculateMode2(sector);
            }
        }

        private static void RecalculateMode1(byte[] sector)
        {
            uint edc = ComputeEdc(sector, 0, 0x810);
            WriteUInt32LE(sector, 0x810, edc);
            Array.Clear(sector, 0x814, 8);
            ComputeEcc(sector, 0x0C, 0x10, 0x81C, 86, 24, 2, 86);
            ComputeEcc(sector, 0x0C, 0x10, 0x8C8, 52, 43, 86, 88);
        }

        private static void RecalculateMode2(byte[] sector)
        {
            bool form2 = (sector[0x12] & 0x20) != 0;
            if (form2)
            {
                uint edc = ComputeEdc(sector, 0x10, 0x91C);
                WriteUInt32LE(sector, 0x92C, edc);
                return;
            }

            uint form1Edc = ComputeEdc(sector, 0x10, 0x808);
            WriteUInt32LE(sector, 0x818, form1Edc);
            ComputeEcc(sector, -1, 0x10, 0x81C, 86, 24, 2, 86);
            ComputeEcc(sector, -1, 0x10, 0x8C8, 52, 43, 86, 88);
        }

        private static uint[] BuildEdcTable()
        {
            uint[] table = new uint[256];
            for (uint i = 0; i < table.Length; i++)
            {
                uint edc = i;
                for (int j = 0; j < 8; j++)
                {
                    edc = (edc >> 1) ^ ((edc & 1) != 0 ? 0xD8018001u : 0u);
                }
                table[i] = edc;
            }
            return table;
        }

        private static uint ComputeEdc(byte[] data, int offset, int count)
        {
            uint edc = 0;
            for (int i = 0; i < count; i++)
            {
                edc = (edc >> 8) ^ EdcTable[(edc ^ data[offset + i]) & 0xFF];
            }
            return edc;
        }

        private static void ComputeEcc(byte[] sector, int addressOffset, int dataOffset, int eccOffset, int majorCount, int minorCount, int majorMult, int minorInc)
        {
            int size = majorCount * minorCount;
            for (int major = 0; major < majorCount; major++)
            {
                int index = (major >> 1) * majorMult + (major & 1);
                byte eccA = 0;
                byte eccB = 0;

                for (int minor = 0; minor < minorCount; minor++)
                {
                    byte value = index < 4
                        ? (addressOffset < 0 ? (byte)0 : sector[addressOffset + index])
                        : sector[dataOffset + index - 4];
                    index += minorInc;
                    if (index >= size) index -= size;
                    eccA ^= value;
                    eccB ^= value;
                    eccA = EccForwardTable[eccA];
                }

                eccA = EccBackwardTable[EccForwardTable[eccA] ^ eccB];
                sector[eccOffset + major] = eccA;
                sector[eccOffset + major + majorCount] = (byte)(eccA ^ eccB);
            }
        }

        private static void WriteUInt32LE(byte[] data, int offset, uint value)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            data[offset + 2] = (byte)(value >> 16);
            data[offset + 3] = (byte)(value >> 24);
        }
    }
}
