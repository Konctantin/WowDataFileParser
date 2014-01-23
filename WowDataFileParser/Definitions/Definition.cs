using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace WowDataFileParser.Definitions
{
    [XmlRoot]
    public class Definition
    {
        /// <summary>
        /// List of file structures.
        /// </summary>
        [XmlElement("file")]
        public List<FileStruct> Files { get; set; }

        [XmlElement("build")]
        public int Build { get; set; }

        public Definition()
        {
            Files = new List<FileStruct>();
        }

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