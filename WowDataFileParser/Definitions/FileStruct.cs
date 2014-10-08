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
        /// Table name.
        /// </summary>
        [XmlAttribute("table")]
        public string TableName   { get; set; }

        /// <summary>
        /// Structure build.
        /// </summary>
        [XmlAttribute("build")]
        public int Build          { get; set; }

        /// <summary>
        /// Field list.
        /// </summary>
        [XmlElement("field")]
        public List<Field> Fields { get; set; }
    }
}