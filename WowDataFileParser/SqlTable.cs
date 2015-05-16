using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WowDataFileParser.Definitions;

namespace WowDataFileParser
{
    internal class SqlTable
    {
        public static void CreateStructure(StreamWriter writer, Definition definition)
        {
            var db_name = "wdb";
            if (definition.Build > 0)
                db_name += "_" + definition.Build;

            writer.WriteLine("-- DB structure");
            writer.WriteLine($"CREATE DATABASE IF NOT EXISTS `{db_name}` CHARACTER SET utf8 COLLATE utf8_general_ci;");
            writer.WriteLine($"USE `{db_name}`;");
            writer.WriteLine();

            // create main tables
            foreach (var file in definition.Files)
            {
                if (string.IsNullOrWhiteSpace(file.Table))
                    throw new NullReferenceException("Table name missing or empty for " + file.Name);

                var keys = new List<string> { "locale" };

                writer.WriteLine($"CREATE TABLE IF NOT EXISTS `{file.Table}` (");
                writer.WriteLine("    `locale` CHAR(4) NOT NULL DEFAULT 'xxXX',");

                foreach (var field in file.Fields.Where(n => n.Type != DataType.TableList))
                    CreateFieldByType(writer, keys, field);

                var key_list = string.Join(", ", keys.Select(key => "`" + key + "`"));
                writer.WriteLine($"    PRIMARY KEY ({key_list})");
                writer.WriteLine(") ENGINE = MyISAM DEFAULT CHARSET = utf8;");
                writer.WriteLine();
            }

            // create sub tables
            foreach (var file in definition.Files)
            {
                if (string.IsNullOrWhiteSpace(file.Table))
                    throw new NullReferenceException("Table name missing or empty for " + file.Name);

                var keys = new List<string> { "locale", "m_entry", "m_index" };

                foreach (var field in file.Fields.Where(n => n.Type == DataType.TableList))
                {
                    if (string.IsNullOrWhiteSpace(field.Name))
                        throw new NullReferenceException("TableList: field name is empty!");

                    writer.WriteLine($"CREATE TABLE IF NOT EXISTS `{field.Name.ToLower()}` (");
                    writer.WriteLine("    `locale` CHAR(4) NOT NULL DEFAULT 'xxXX',");
                    writer.WriteLine("    `m_entry` INT UNSIGNED NOT NULL DEFAULT '0',");
                    writer.WriteLine("    `m_index` INT UNSIGNED NOT NULL DEFAULT '0',");

                    foreach (var subField in field.Fields)
                    {
                        CreateFieldByType(writer, keys, subField);
                    }

                    var key_list = string.Join(", ", keys.Select(key => "`" + key + "`"));
                    writer.WriteLine($"    PRIMARY KEY ({key_list})");
                    writer.WriteLine(") ENGINE = MyISAM DEFAULT CHARSET = utf8;");
                    writer.WriteLine();
                }
            }
        }

        private static void CreateFieldByType(StreamWriter writer, List<string> keys, Field field, string suffix = "")
        {
            if (field.Type == DataType.None)
                return;

            if (field.Key)
                keys.Add(field.Name);


            #region Type
            switch (field.Type)
            {
                case DataType.Long:
                    writer.WriteLine("    `{0}` BIGINT NOT NULL DEFAULT '0',", field.Name.ToLower() + suffix);
                    break;
                case DataType.Ulong:
                    writer.WriteLine("    `{0}` BIGINT UNSIGNED NOT NULL DEFAULT '0',", field.Name.ToLower() + suffix);
                    break;
                case DataType.Int:
                    writer.WriteLine("    `{0}` INT NOT NULL DEFAULT '0',", field.Name.ToLower() + suffix);
                    break;
                case DataType.Uint:
                    writer.WriteLine("    `{0}` INT UNSIGNED NOT NULL DEFAULT '0',", field.Name.ToLower() + suffix);
                    break;
                case DataType.Short:
                    writer.WriteLine("    `{0}` SMALLINT NOT NULL DEFAULT '0',", field.Name.ToLower() + suffix);
                    break;
                case DataType.Ushort:
                    writer.WriteLine("    `{0}` SMALLINT UNSIGNED NOT NULL DEFAULT '0',", field.Name.ToLower() + suffix);
                    break;
                case DataType.Byte:
                    writer.WriteLine("    `{0}` TINYINT NOT NULL DEFAULT '0',", field.Name.ToLower() + suffix);
                    break;
                case DataType.SByte:
                    writer.WriteLine("    `{0}` TINYINT UNSIGNED NOT NULL DEFAULT '0',", field.Name.ToLower() + suffix);
                    break;
                case DataType.Float:
                    writer.WriteLine("    `{0}` FLOAT NOT NULL DEFAULT '0',", field.Name.ToLower() + suffix);
                    break;
                case DataType.Double:
                    writer.WriteLine("    `{0}` DOUBLE NOT NULL DEFAULT '0',", field.Name.ToLower() + suffix);
                    break;
                case DataType.String:
                case DataType.String2:
                case DataType.Pstring:
                    {
                        var maxLen = (int)Math.Pow(2, field.Size);
                        if (maxLen > 8000 || maxLen <= 1)
                            writer.WriteLine("    `{0}` TEXT,", field.Name.ToLower() + suffix);
                        else
                            writer.WriteLine("    `{0}` VARCHAR({1}),", field.Name.ToLower() + suffix, maxLen);
                    } break;
                case DataType.List:
                    {
                        if (field.Size > 0)
                        {
                            var fname = field.Name.ToLower();
                            if (!char.IsDigit(fname[fname.Length - 1]))
                            {
                                if (suffix.Length > 0 && suffix[0] == '_')
                                    fname += suffix.Substring(1);
                                else
                                    fname += suffix;
                            }
                            else { fname += suffix; }

                            writer.WriteLine("    `{0}` INT NOT NULL DEFAULT '0',", fname);
                        }

                        for (int i = 0; i < field.Maxsize; ++i)
                        {
                            var m_suffix = suffix + "_" + (i + 1);
                            foreach (var element in field.Fields)
                                CreateFieldByType(writer, keys, element, m_suffix);
                        }
                    }
                    break;
                case DataType.StringList:
                    {
                        foreach (var subField in field.Fields)
                        {
                            var maxLen = (int)Math.Pow(2, subField.Size);

                            if (maxLen > 8000 || maxLen <= 1)
                                writer.WriteLine("    `{0}` TEXT,", subField.Name.ToLower() + suffix);
                            else
                                writer.WriteLine("    `{0}` VARCHAR({1}),", subField.Name.ToLower() + suffix, maxLen);
                        }
                    } break;
                default:
                    throw new Exception("Unknown field type " + field.Type);
            }
            #endregion
        }
    }
}