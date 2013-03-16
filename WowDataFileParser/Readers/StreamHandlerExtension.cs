using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Kamilla.IO;

namespace WDReader.Reader
{
    public static class StreamHandlerExtension
    {
        public static StreamHandler DecompressBlock(this StreamHandler reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            int uncompressedLength = reader.ReadInt32();
            if (uncompressedLength == 0)
                return new StreamHandler(new byte[0]);

            reader.Skip(2);
            byte[] compressedBytes = reader.ReadBytes((int)reader.RemainingLength);

            byte[] uncompressedBytes = new byte[uncompressedLength];
            using (var stream = new MemoryStream(compressedBytes))
            using (var dStream = new DeflateStream(stream, CompressionMode.Decompress, true))
                dStream.Read(uncompressedBytes, 0, uncompressedLength);

            return new StreamHandler(uncompressedBytes);
        }

        public static StreamHandler ReadHasByte(this StreamHandler reader, out byte value)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            value = reader.UnalignedReadBit() ? (byte)1 : (byte)0;

            return reader;
        }

        public static StreamHandler ReadXorByte(this StreamHandler reader, ref byte value)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            if (value != 0)
            {
                if (value != 1)
                    throw new InvalidOperationException();

                value ^= reader.ReadByte();
            }

            return reader;
        }

        public static StreamHandler WriteHasByte(this StreamHandler writer, byte value)
        {
            if (writer == null)
                throw new ArgumentNullException("reader");

            writer.UnalignedWriteBit(value != 0);

            return writer;
        }

        public static StreamHandler WriteXorByte(this StreamHandler writer, byte value)
        {
            if (writer == null)
                throw new ArgumentNullException("reader");

            if (value != 0)
                writer.WriteByte((byte)(value ^ 1));

            return writer;
        }

        public static string ReadPascalString32(this StreamHandler sh)
        {
            return PascalStringReader(sh, sh.ReadInt32());
        }

        public static string ReadPascalString16(this StreamHandler sh)
        {
            return PascalStringReader(sh, sh.ReadUInt16());
        }

        public static string ReadPascalString8(this StreamHandler sh)
        {
            return PascalStringReader(sh, sh.ReadByte());
        }

        public static string ReadPascalString12Bit(this StreamHandler sh)
        {
            var val1 = sh.UnalignedReadTinyInt(8);
            var val2 = sh.UnalignedReadTinyInt(4);

            var len = 16 * val1 | val2;

            if (len == 0)
                return string.Empty;

            return PascalStringReader(sh, len - 1);
        }

        private static string PascalStringReader(StreamHandler sh, int length)
        {
            if (length > 0)
            {
                byte[] bytes = sh.ReadBytes(length + 1);

                int len = length + 1;
                if (bytes[bytes.Length - 1] == 0x00)
                    --len;

                return Encoding.UTF8.GetString(bytes, 0, len);
            }
            else
                return string.Empty;
        }

        #region String Extensions

        public static string EscapeSqlSumbols(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return "NULL";
            return @"'" + str.Replace(@"'", @"\'").Replace("\"", "\\\"") + @"'";
        }

        #endregion
    }
}
