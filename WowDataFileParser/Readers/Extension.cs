using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace WDReader.Reader
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
