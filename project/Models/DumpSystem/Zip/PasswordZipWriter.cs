using System;
using System.IO;
using System.Text;

namespace project.DumpSystem.Zip
{
    public static class PasswordZipWriter
    {
        public static void CreateEncryptedZip(string zipPath, string sourceFilePath, string entryName, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Пароль для архива не задан.");
            }

            byte[] fileBytes = File.ReadAllBytes(sourceFilePath);
            byte[] nameBytes = Encoding.UTF8.GetBytes(entryName);
            uint crc = Crc32.Compute(fileBytes);
            ushort dosTime = GetDosTime(File.GetLastWriteTime(sourceFilePath));
            ushort dosDate = GetDosDate(File.GetLastWriteTime(sourceFilePath));
            byte[] encryptedData = EncryptWithHeader(fileBytes, password, crc);

            string directory = Path.GetDirectoryName(zipPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                long localHeaderOffset = stream.Position;
                WriteLocalHeader(writer, nameBytes, crc, (uint)encryptedData.Length, (uint)fileBytes.Length, dosTime, dosDate);
                writer.Write(nameBytes);
                writer.Write(encryptedData);

                long centralDirectoryOffset = stream.Position;
                WriteCentralDirectory(writer, nameBytes, crc, (uint)encryptedData.Length, (uint)fileBytes.Length, dosTime, dosDate, (uint)localHeaderOffset);
                long centralDirectorySize = stream.Position - centralDirectoryOffset;

                WriteEndOfCentralDirectory(writer, 1, (uint)centralDirectorySize, (uint)centralDirectoryOffset);
            }
        }

        public static bool ValidateEncryptedZip(string zipPath, string password)
        {
            try
            {
                using (var stream = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    uint signature = reader.ReadUInt32();
                    if (signature != 0x04034b50)
                    {
                        return false;
                    }

                    reader.ReadUInt16();
                    ushort flags = reader.ReadUInt16();
                    ushort method = reader.ReadUInt16();
                    reader.ReadUInt16();
                    reader.ReadUInt16();
                    uint crc = reader.ReadUInt32();
                    uint compressedSize = reader.ReadUInt32();
                    uint uncompressedSize = reader.ReadUInt32();
                    ushort fileNameLength = reader.ReadUInt16();
                    ushort extraLength = reader.ReadUInt16();

                    if ((flags & 1) == 0 || method != 0 || compressedSize < 12)
                    {
                        return false;
                    }

                    stream.Position += fileNameLength + extraLength;
                    byte[] encrypted = reader.ReadBytes((int)compressedSize);
                    byte[] decrypted = DecryptWithHeader(encrypted, password, crc);

                    if (decrypted.Length != uncompressedSize)
                    {
                        return false;
                    }

                    return Crc32.Compute(decrypted) == crc;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void WriteLocalHeader(BinaryWriter writer, byte[] nameBytes, uint crc, uint compressedSize, uint uncompressedSize, ushort dosTime, ushort dosDate)
        {
            writer.Write(0x04034b50u);
            writer.Write((ushort)20);
            writer.Write((ushort)0x0801); 
            writer.Write((ushort)0);
            writer.Write(dosTime);
            writer.Write(dosDate);
            writer.Write(crc);
            writer.Write(compressedSize);
            writer.Write(uncompressedSize);
            writer.Write((ushort)nameBytes.Length);
            writer.Write((ushort)0);
        }

        private static void WriteCentralDirectory(BinaryWriter writer, byte[] nameBytes, uint crc, uint compressedSize, uint uncompressedSize, ushort dosTime, ushort dosDate, uint localHeaderOffset)
        {
            writer.Write(0x02014b50u);
            writer.Write((ushort)20);
            writer.Write((ushort)20);
            writer.Write((ushort)0x0801);
            writer.Write((ushort)0);
            writer.Write(dosTime);
            writer.Write(dosDate);
            writer.Write(crc);
            writer.Write(compressedSize);
            writer.Write(uncompressedSize);
            writer.Write((ushort)nameBytes.Length);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((uint)0);
            writer.Write(localHeaderOffset);
            writer.Write(nameBytes);
        }

        private static void WriteEndOfCentralDirectory(BinaryWriter writer, ushort entries, uint centralDirectorySize, uint centralDirectoryOffset)
        {
            writer.Write(0x06054b50u);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(entries);
            writer.Write(entries);
            writer.Write(centralDirectorySize);
            writer.Write(centralDirectoryOffset);
            writer.Write((ushort)0);
        }

        private static byte[] EncryptWithHeader(byte[] data, string password, uint crc)
        {
            var crypto = new ZipCrypto(password);
            byte[] result = new byte[data.Length + 12];
            byte[] header = new byte[12];
            var random = new Random();

            for (int i = 0; i < 11; i++)
            {
                header[i] = (byte)random.Next(0, 256);
            }

            header[11] = (byte)(crc >> 24);

            for (int i = 0; i < header.Length; i++)
            {
                result[i] = crypto.EncryptByte(header[i]);
            }

            for (int i = 0; i < data.Length; i++)
            {
                result[i + 12] = crypto.EncryptByte(data[i]);
            }

            return result;
        }

        private static byte[] DecryptWithHeader(byte[] encryptedData, string password, uint crc)
        {
            var crypto = new ZipCrypto(password);
            byte[] header = new byte[12];

            for (int i = 0; i < 12; i++)
            {
                header[i] = crypto.DecryptByte(encryptedData[i]);
            }

            if (header[11] != (byte)(crc >> 24))
            {
                throw new InvalidDataException("Неверный пароль или поврежденный архив.");
            }

            byte[] data = new byte[encryptedData.Length - 12];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = crypto.DecryptByte(encryptedData[i + 12]);
            }

            return data;
        }

        private static ushort GetDosTime(DateTime time)
        {
            return (ushort)((time.Hour << 11) | (time.Minute << 5) | (time.Second / 2));
        }

        private static ushort GetDosDate(DateTime time)
        {
            int year = Math.Max(1980, time.Year) - 1980;
            return (ushort)((year << 9) | (time.Month << 5) | time.Day);
        }
    }

    internal class ZipCrypto
    {
        private uint _key0 = 0x12345678;
        private uint _key1 = 0x23456789;
        private uint _key2 = 0x34567890;

        public ZipCrypto(string password)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
            for (int i = 0; i < bytes.Length; i++)
            {
                UpdateKeys(bytes[i]);
            }
        }

        public byte EncryptByte(byte value)
        {
            byte result = (byte)(value ^ GetMagicByte());
            UpdateKeys(value);
            return result;
        }

        public byte DecryptByte(byte value)
        {
            byte result = (byte)(value ^ GetMagicByte());
            UpdateKeys(result);
            return result;
        }

        private byte GetMagicByte()
        {
            ushort temp = (ushort)(_key2 | 2);
            return (byte)((temp * (temp ^ 1)) >> 8);
        }

        private void UpdateKeys(byte value)
        {
            _key0 = Crc32.Update(_key0, value);
            _key1 = _key1 + (byte)_key0;
            _key1 = _key1 * 134775813 + 1;
            _key2 = Crc32.Update(_key2, (byte)(_key1 >> 24));
        }
    }

    internal static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        public static uint Compute(byte[] data)
        {
            uint crc = 0xffffffff;
            for (int i = 0; i < data.Length; i++)
            {
                crc = Update(crc, data[i]);
            }

            return crc ^ 0xffffffff;
        }

        public static uint Update(uint crc, byte value)
        {
            return (crc >> 8) ^ Table[(crc ^ value) & 0xff];
        }

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < table.Length; i++)
            {
                uint value = i;
                for (int j = 0; j < 8; j++)
                {
                    value = (value & 1) == 1 ? 0xedb88320 ^ (value >> 1) : value >> 1;
                }

                table[i] = value;
            }

            return table;
        }
    }
}
