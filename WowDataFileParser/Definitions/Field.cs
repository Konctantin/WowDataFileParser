using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;

namespace WowDataFileParser.Definitions
{
    public class Field
    {
        [XmlAttribute("name")]
        public string Name        { get; set; }

        [XmlAttribute("type")]
        public DataType Type      { get; set; }

        [XmlAttribute("size")]
        public int Size           { get; set; }

        [XmlAttribute("sizelink")]
        public string SizeLink    { get; set; }

        [XmlAttribute("key")]
        public bool Key           { get; set; }

        [XmlAttribute("max")]
        public int Max            { get; set; }

        [XmlAttribute("maxsize")]
        public int Maxsize        { get; set; }

        [XmlElement("field")]
        public List<Field> Fields { get; set; }

        [XmlIgnore]
        public object Value       { get; set; }

        public string GetEscapedSqlValue()
        {
            if (Value == null)
                return "NULL";
            else if (Value is string)
            {
                var str = (Value as string);
                if (string.IsNullOrWhiteSpace(str) || str == "\0")
                    return "NULL";
                return "\'" + str.Replace(@"'", @"\'").Replace("\"", "\\\"") + "\'";
            }
            else if (Value is bool)     return ((bool)   Value ? "1" : "0");
            else if (Value is byte)     return ((byte)   Value).ToString();
            else if (Value is sbyte)    return ((sbyte)  Value).ToString();
            else if (Value is short)    return ((short)  Value).ToString();
            else if (Value is ushort)   return ((ushort) Value).ToString();
            else if (Value is int)      return ((int)    Value).ToString();
            else if (Value is uint)     return ((uint)   Value).ToString();
            else if (Value is long)     return ((long)   Value).ToString();
            else if (Value is ulong)    return ((ulong)  Value).ToString();
            else if (Value is float)    return (((float) Value).ToString(CultureInfo.InvariantCulture));
            else if (Value is double)   return (((double)Value).ToString(CultureInfo.InvariantCulture));
            else
                throw new NotSupportedException("Not suported type " + Value.GetType());
        }
    }
}