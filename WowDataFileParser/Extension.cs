using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WowDataFileParser
{
    public static class Extension
    {
        public static string ReadCString(this BinaryReader reader)
        {
            List<byte> list = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0)
                list.Add(b);
            return Encoding.UTF8.GetString(list.ToArray());
        }

        public static string ReadReverseString(this BinaryReader reader, int count)
        {
            if (reader.BaseStream.Position + count > reader.BaseStream.Length)
                throw new ArgumentOutOfRangeException("count");

            return Encoding.ASCII.GetString(reader.ReadBytes(count).Reverse().ToArray());
        }
    }
}
