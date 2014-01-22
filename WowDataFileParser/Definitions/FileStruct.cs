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
        /// Parsed filename
        /// </summary>
        [XmlAttribute("name")]
        public string Name        { get; set; }

        /// <summary>
        /// Table name.
        /// </summary>
        [XmlAttribute("table")]
        public string TableName   { get; set; }

        /// <summary>
        /// Cleint build.
        /// </summary>
        [XmlAttribute("build")]
        public int Build          { get; set; }

        /// <summary>
        /// Field list.
        /// </summary>
        [XmlElement("field")]
        public List<Field> Fields { get; set; }

        public FileStruct()
        {
            Fields = new List<Field>();
        }

        private void Clean(Field field)
        {
            field.Value = null;
            if (field.Fields != null)
            {

                foreach (var item in field.Fields)
                {
                    Clean(item);
                }
            }
        }

        public void Init()
        {
            foreach (var item in this.Fields)
                Clean(item);
        }

        private void GetValue(ref StringBuilder content, Field field)
        {
            var val = field.GetEscapedSqlValue();

            if (field.Type != DataType.List || field.Value != null)
            {
                content.Append(val);
                content.Append(", ");
            }

            if (field.Fields != null)
            {
                foreach (var item in field.Fields)
                {
                    GetValue(ref content, item);
                }
            }
        }

        public string ToSqlString(string locale)
        {
            var content = new StringBuilder();
            content.AppendFormat("REPLACE INTO `{0}` VALUES (\'{1}\', ",  Name, locale);

            foreach (var field in this.Fields)
                GetValue(ref content, field);

            return content.
                Remove(content.Length - 2, 2)
                .Append(");")
                .ToString();
        }
    }
}