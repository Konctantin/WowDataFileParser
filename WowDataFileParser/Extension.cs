using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using WowDataFileParser.Definitions;

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

        public static int GetValueByName(this IList<Field> collection, string name)
        {
            foreach (var item in collection)
                if (item.Name == name)
                    return Convert.ToInt32(item.Value);
            return 0;
        }
    }
}
