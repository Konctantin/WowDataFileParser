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

        public FileStruct this[string name, uint build]
        {
            get
            {
                var list = Files
                    .Where(n => n.Name == name && (n.Build == 0 || n.Build <= build))
                    .OrderByDescending(n => n.Build);
                if (list.Count() > 0)
                    return list.FirstOrDefault();
                return null;
            }
        }
    }
}