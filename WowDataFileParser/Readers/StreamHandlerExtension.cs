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

        public static string ReadPascalString(this StreamHandler sh, int length)
        {
            if (length > 0)
            {
                byte[] bytes = sh.ReadBytes(length);
                return Encoding.UTF8.GetString(bytes);
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
