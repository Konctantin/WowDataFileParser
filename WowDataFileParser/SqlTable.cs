using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WowDataFileParser.Definitions;

namespace WowDataFileParser
{
    internal class SqlTable
    {
        public static void CreateSqlTable(Definition definition)
        {
            var writer = new StreamWriter(string.Format("table_structure.sql"));

            writer.WriteLine("DROP DATABASE IF EXISTS `wdb`;");
            writer.WriteLine("CREATE DATABASE `wdb` CHARACTER SET utf8 COLLATE utf8_general_ci;");
            writer.WriteLine("USE `wdb`;");
                
            Console.Write("║ ");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write("wdb".PadRight(34));
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

                writer.WriteLine("-- {0} structure", element.TableName);
                writer.WriteLine("CREATE TABLE `{0}` (", element.TableName);
                writer.WriteLine("    `locale`                        char(4) NOT NULL,");

                foreach (var record in element.Fields)
                    WriteFieldByType(writer, keys, record, string.Empty);

                writer.WriteLine("    PRIMARY KEY (" + string.Join(", ", keys.Select(k => "`" + k + "`")) + ")");
                writer.WriteLine(") ENGINE = MyISAM DEFAULT CHARSET = utf8 COMMENT = 'Export of {0}';", element.TableName);
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
                    throw new Exception("Unknown field type " + record.Type);
            }
            #endregion
        }
    }
}