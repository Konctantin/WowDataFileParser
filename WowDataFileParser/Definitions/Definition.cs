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

        public Definition()
        {
            Files = new List<FileStruct>();
        }

        public FileStruct GetStructure(string name, uint build)
        {
            var list = Files.Where(n => n.Name == name && n.Build <= build);
            if (list.Count() > 0)
                return list.OrderBy(n => n.Build).FirstOrDefault();
            return null; 
        }
    }
}