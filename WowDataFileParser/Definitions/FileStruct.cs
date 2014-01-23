﻿using System;
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
    }
}