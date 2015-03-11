using System.Collections.Generic;
using System.Xml.Serialization;

namespace WowDataFileParser.Definitions
{
    public class FileStruct
    {
        /// <summary>
        /// Parsed filename.
        /// </summary>
        [XmlAttribute("name")]
        public string Name        { get; set; }

        [XmlAttribute("table")]
        public string Table       { get; set; }

        /// <summary>
        /// Field list.
        /// </summary>
        [XmlElement("field")]
        public List<Field> Fields { get; set; }
    }
}