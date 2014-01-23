using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using WowDataFileParser.Definitions;

namespace WowDataFileParser
{
    internal class SqlTable
    {
        public static void CreateSqlTable(Definition definition)
        {
            if (definition.Build == 0)
                throw new Exception("build is empty");

            var writer = new StreamWriter(string.Format("table_structure_{0}.sql", definition.Build));

            writer.WriteLine("DROP DATABASE IF EXISTS `wdb_{0}`;", definition.Build);
            writer.WriteLine("CREATE DATABASE `wdb_{0}` CHARACTER SET utf8 COLLATE utf8_general_ci;", definition.Build);
            writer.WriteLine("USE `wdb_{0}`;", definition.Build);
                
            Console.Write("║ ");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write("wdb_{0, -30}", definition.Build);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("║ ");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write("{0,-16}", "database");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("║");

            int index = 0;
            foreach (var element in definition.Files)
            {
                var keys = new List<string>();
                keys.Add("locale");

                writer.WriteLine("-- {0} structure for build {1}", element.TableName, definition.Build);
                writer.WriteLine("DROP TABLE IF EXISTS `{0}`;", element.TableName);
                writer.WriteLine("CREATE TABLE `{0}` (", element.TableName);
                writer.WriteLine("    `locale`                        char(4) default NULL,");

                foreach (var record in element.Fields)
                    WriteFieldByType(writer, keys, record, string.Empty);

                writer.WriteLine("    PRIMARY KEY (" + string.Join(", ", keys.Select(k => "`" + k + "`")) + ")");
                writer.WriteLine(") ENGINE=MyISAM DEFAULT CHARSET=utf8 COMMENT='Export of {0}';", element.TableName);
                writer.WriteLine();
                // hack - last is empty (-1)
                if (index < element.Fields.Count - 2)
                    Console.WriteLine("║  ╞═ {0, -30}║ {1,-16}║", element.TableName, "table");
                else
                    Console.WriteLine("║  ╘═ {0, -30}║ {1,-16}║", element.TableName, "table");
                ++index;
            }
            writer.Flush();
            writer.Close();
        }

        private static void WriteFieldByType(StreamWriter writer, List<string> keys, Field record, string suffix)
        {
            if (record.Type == DataType.None)
                return;

            if (record.Key)
                keys.Add(record.Name);

            #region Type
            switch (record.Type)
            {
                case DataType.Long:
                    writer.WriteLine("    `{0,-30} BIGINT NOT NULL DEFAULT '0',", record.Name + suffix + '`');
                    break;
                case DataType.Ulong:
                    writer.WriteLine("    `{0,-30} BIGINT UNSIGNED NOT NULL DEFAULT '0',", record.Name + suffix + '`');
                    break;
                case DataType.Int:
                    writer.WriteLine("    `{0,-30} INT NOT NULL DEFAULT '0',", record.Name + suffix + '`');
                    break;
                case DataType.Uint:
                    writer.WriteLine("    `{0,-30} INT UNSIGNED NOT NULL DEFAULT '0',", record.Name + suffix + '`');
                    break;
                case DataType.Short:
                    writer.WriteLine("    `{0,-30} SMALLINT NOT NULL DEFAULT '0',", record.Name + suffix + '`');
                    break;
                case DataType.Ushort:
                    writer.WriteLine("    `{0,-30} SMALLINT UNSIGNED NOT NULL DEFAULT '0',", record.Name + suffix + '`');
                    break;
                case DataType.Byte:
                    writer.WriteLine("    `{0,-30} TINYINT NOT NULL DEFAULT '0',", record.Name + suffix + '`');
                    break;
                case DataType.Float:
                    writer.WriteLine("    `{0,-30} FLOAT NOT NULL DEFAULT '0',", record.Name + suffix + '`');
                    break;
                case DataType.Double:
                    writer.WriteLine("    `{0,-30} DOUBLE NOT NULL DEFAULT '0',", record.Name + suffix + '`');
                    break;
                case DataType.String:
                case DataType.String2:
                case DataType.Pstring:
                    writer.WriteLine("    `{0,-30} TEXT,", record.Name + suffix + '`');
                    break;
                case DataType.List:
                    {
                        if (!string.IsNullOrWhiteSpace(record.Name))
                        {
                            var counttype = record.Name;

                            var fname = record.Name;
                            if (!char.IsDigit(fname[fname.Length - 1]))
                            {
                                if (suffix.Length > 0 && suffix[0] == '_')
                                    fname += suffix.Substring(1);
                                else
                                    fname += suffix;
                            }
                            else { fname += suffix; }

                            writer.WriteLine("    `{0,-30} INT NOT NULL DEFAULT '0',", fname + "`");
                        }

                        for (int i = 0; i < record.Maxsize; ++i)
                        {
                            var m_suffix = suffix + "_" + (i + 1);
                            foreach (var element in record.Fields)
                                WriteFieldByType(writer, keys, element, m_suffix);
                        }
                    }
                    break;
                default:
                    throw new Exception(string.Format("Unknown field type {0}!", record.Type));
            }
            #endregion
        }
    }
}