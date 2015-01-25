using System.Collections.Generic;
using System.Xml.Serialization;

namespace WowDataFileParser.Definitions
{
    public class Field
    {
        [XmlAttribute("name")]
        public string Name        { get; set; }

        [XmlAttribute("type")]
        public DataType Type      { get; set; }

        [XmlAttribute("key")]
        public bool Key           { get; set; }

        [XmlAttribute("entry")]
        public string KeyFieldName { get; set; }

        [XmlAttribute("maxsize")]
        public int Maxsize        { get; set; }

        [XmlElement("field")]
        public List<Field> Fields { get; set; }

        [XmlIgnore]
        public int Size        { get; private set; }

        [XmlIgnore]
        public string SizeLink { get; private set; }

        private string rawSize;
        [XmlAttribute("size")]
        public string RawSize
        {
            get { return rawSize; }
            set
            {
                rawSize = value;
                int size;
                if (!int.TryParse(value, out size))
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        SizeLink = value;
                }
                Size = size;
            }
        }
    }
}