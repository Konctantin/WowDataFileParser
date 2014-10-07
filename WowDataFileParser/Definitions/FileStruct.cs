using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
        public int Build { get; set; }

        /// <summary>
        /// Field list.
        /// </summary>
        [XmlElement("field")]
        public List<Field> Fields { get; set; }

        public FileStruct()
        {
            Fields = new List<Field>();
        }

        public FileStruct Copy()
        {
            var copy = new FileStruct {
                Name      = this.Name,
                TableName = this.TableName,
                Build     = this.Build,
                Fields    = new List<Field>()
            };

            foreach (var item in this.Fields)
                copy.Fields.Add(item.Copy());

            return copy;
        }
    }
}