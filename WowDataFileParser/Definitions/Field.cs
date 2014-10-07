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
        public IConvertible Value { get; set; }

        public Field Copy()
        {
            var copy = new Field {
                Name     = this.Name,
                Type     = this.Type,
                Size     = this.Size,
                SizeLink = this.SizeLink,
                Key      = this.Key,
                Max      = this.Max,
                Maxsize  = this.Maxsize,
                Fields   = new List<Field>()
            };

            foreach (var item in this.Fields)
                copy.Fields.Add(item.Copy());

            return copy;
        }
    }
}