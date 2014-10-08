using System.Collections.Generic;
using System.Linq;
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

        public FileStruct this[string name]
        {
            get
            {
                var list = Files.Where(n => n.Name == name);
                if (list.Count() > 0)
                    return list.FirstOrDefault();
                return null;
            }
        }
    }
}