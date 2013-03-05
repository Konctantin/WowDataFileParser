using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace WowDataFileParser
{
    internal class StaructureTable
    {
        internal StaructureTable()
        {
            var xml = new XmlDocument();
            xml.Load(Parser.DEF);

            XmlNode root = xml.GetElementsByTagName("Definitions")[0];
            if (root.Attributes["build"] == null)
                throw new Exception("build is empty");

            string build = root.Attributes["build"].Value;
            string outputFileName = string.Format("table_structure_{0}.sql", build);
            
            var writer = new StreamWriter(outputFileName);

            writer.WriteLine("DROP DATABASE IF EXISTS `wdb_{0}`;", build);
            writer.WriteLine("CREATE DATABASE `wdb_{0}` CHARACTER SET utf8 COLLATE utf8_general_ci;", build);
            writer.WriteLine("USE `wdb_{0}`;", build);
                
            Console.Write("║ ");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write("wdb_{0, -30}", build);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("║ ");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write("{0,-16}", "database");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("║");

            var collection = xml.GetElementsByTagName("Definitions")[0].OfType<XmlElement>();
            int index = 0;
            foreach (XmlElement element in collection)
            {
                if (element.Attributes["tablename"] == null)
                    continue;
                List<string> keys = new List<string>();
                keys.Add("locale");
                string tableName = element.Attributes["tablename"].Value;
                writer.WriteLine("-- {0} structure for build {1}", tableName, build);
                writer.WriteLine("DROP TABLE IF EXISTS `{0}`;", tableName);
                writer.WriteLine("CREATE TABLE `{0}` (", tableName);
                writer.WriteLine("    `locale`                        char(5) default NULL,");
                foreach (XmlElement record in element.ChildNodes.OfType<XmlElement>())
                {
                    WriteFieldByType(writer, keys, record, string.Empty);
                }
                writer.Write("    PRIMARY KEY (");
                for (int i = 0; i < keys.Count; ++i)
                    writer.Write("`{0}`{1}", keys[i], (i == keys.Count - 1)? ")" + Environment.NewLine: ", ");
                writer.WriteLine(") ENGINE=MyISAM DEFAULT CHARSET=utf8 COMMENT='Export of {0}';", tableName);
                writer.WriteLine();
                // hack - last is empty (-1)
                if (index < collection.Count() - 2)
                    Console.WriteLine("║  ╞═ {0, -30}║ {1,-16}║", tableName, "table");
                else
                    Console.WriteLine("║  ╘═ {0, -30}║ {1,-16}║", tableName, "table");
                ++index;
            }
            writer.Flush();
            writer.Close();
        }

        private void WriteFieldByType(StreamWriter writer, List<string> keys, XmlElement record, string suffix)
        {
            Action writeList = () => {
                int elementCount = int.Parse(record.Attributes["maxcount"].Value);

                if (record.Attributes["name"] != null)
                {
                    var counttype = record.Attributes["name"].Value;
                    var fname = record.Attributes["name"].Value;
                    if (char.IsDigit(fname[fname.Length - 1]))
                    {
                        if (suffix[0] == '_')
                            fname += suffix.Substring(1);
                        else
                            fname += suffix;
                    }
                    else { fname += suffix; }

                    writer.WriteLine("    `{0,-30} INT NOT NULL DEFAULT '0',", fname + "`");
                }

                for (int i = 0; i < elementCount; ++i)
                {
                    var m_suffix = suffix + "_" + (i + 1);
                    foreach (XmlElement element in record.ChildNodes.OfType<XmlElement>())
                    {
                        WriteFieldByType(writer, keys, element, m_suffix);
                    }
                }
            };

            if (record.Attributes["type"] == null)
                return;

            if (record.Attributes["key"] != null)
                keys.Add(record.Attributes["name"].Value);

            string fieldType = record.Attributes["type"].Value;

            if (record.Attributes["name"] == null)
            {
                if (fieldType.ToLower() == "list")
                    writeList();
                return;
            }

            var fieldName = record.Attributes["name"].Value;

            if (fieldType.ToLower() != "list")
                writer.Write("    `{0,-30}", fieldName + suffix + '`');

            #region Type
            switch (fieldType.ToLower())
            {
                case "long":
                    writer.WriteLine(" BIGINT NOT NULL DEFAULT '0',");
                    break;
                case "ulong":
                    writer.WriteLine(" BIGINT UNSIGNED NOT NULL DEFAULT '0',");
                    break;
                case "int":
                    writer.WriteLine(" INT NOT NULL DEFAULT '0',");
                    break;
                case "uint":
                    writer.WriteLine(" INT UNSIGNED NOT NULL DEFAULT '0',");
                    break;
                case "short":
                    writer.WriteLine(" SMALLINT NOT NULL DEFAULT '0',");
                    break;
                case "ushort":
                    writer.WriteLine(" SMALLINT UNSIGNED NOT NULL DEFAULT '0',");
                    break;
                case "sbyte":
                    writer.WriteLine(" TINYINT NOT NULL DEFAULT '0',");
                    break;
                case "byte":
                    writer.WriteLine(" TINYINT UNSIGNED NOT NULL DEFAULT '0',");
                    break;
                case "float":
                    writer.WriteLine(" FLOAT NOT NULL DEFAULT '0',");
                    break;
                case "double":
                    writer.WriteLine(" DOUBLE NOT NULL DEFAULT '0',");
                    break;
                case "string":
                    writer.WriteLine(" TEXT NOT NULL,");
                    break;
                case "list":
                    writeList();
                    break;
                default:
                    throw new Exception(String.Format("Unknown field type {0}!", fieldType));
            }
            #endregion
        }
    }
}