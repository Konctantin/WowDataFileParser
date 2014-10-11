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

        /// <summary>
        /// Field list.
        /// </summary>
        [XmlElement("field")]
        public List<Field> Fields { get; set; }

        [XmlIgnore]
        public string TableName
        {
            get { return Name.Replace('-', '_').Replace('.', '_').ToLower(); }
        }
    }
}