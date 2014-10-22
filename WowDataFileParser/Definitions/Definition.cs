using System.Collections.Generic;
using System.Xml.Serialization;

namespace WowDataFileParser.Definitions
{
    [XmlRoot]
    public class Definition
    {
        /// <summary>
        /// Cache version.
        /// </summary>
        [XmlElement("build")]
        public int Build { get; set; }

        /// <summary>
        /// List of file structures.
        /// </summary>
        [XmlElement("file")]
        public List<FileStruct> Files { get; set; }
    }
}