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
    }
}