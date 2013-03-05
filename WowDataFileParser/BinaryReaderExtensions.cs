using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WowDataFileParser
{
    static class BinaryReaderExtensions
    {
        public static string GetFileName(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return string.Empty;
            return str.Replace(Environment.CurrentDirectory, string.Empty);
        }

        /// <summary>
        ///  Reads the NULL terminated string from the current stream and advances the current position of the stream by string length + 1.
        /// <seealso cref="GenericReader.ReadStringNumber"/>
        /// </summary>
        public static string ReadCString(this BinaryReader reader)
        {
            byte num;
            var text = string.Empty;
            var temp = new List<byte>();

            while ((num = reader.ReadByte()) != 0)
                temp.Add(num);

            text = Encoding.UTF8.GetString(temp.ToArray());

            return text;
        }
    }
}