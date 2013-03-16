using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using WDReader.Reader;
using Kamilla.IO;

namespace WowDataFileParser
{
    internal class Parser
    {
        public static readonly string[] FILE_FILTER = { ".wdb", ".adb", ".dbc", ".db2" };
        public static readonly string WDB_FOLDER    = "wdb";
        public static readonly string DEF           = "definitions.xml";
        public static readonly string OUTPUT_FILE   = "output.sql";

        private StreamHandler RowReader;
        private XmlDocument  xmlstruct;

        internal Parser()
        {
            if (!File.Exists(DEF))
                throw new FileNotFoundException(DEF);

            var files = new DirectoryInfo(Environment.CurrentDirectory)
                .GetFiles("*.*", SearchOption.AllDirectories);
            
            if (files.Count() == 0)
                throw new Exception("Folder is Empty");

            xmlstruct = new XmlDocument();
            xmlstruct.Load(DEF);

            try
            {
                File.Delete(OUTPUT_FILE);
            }
            catch
            {
                throw new Exception("File " + OUTPUT_FILE + " used by another application!");
            }

            using (var writer = new StreamWriter(OUTPUT_FILE, false))
            {
                foreach (var file in files)
                {
                    if (!FILE_FILTER.Contains(file.Extension))
                        continue;

                    var file_name = Path.GetFileNameWithoutExtension(file.Name);
                    foreach (XmlElement element in xmlstruct.GetElementsByTagName(file_name))
                    {
                        BaseReader reader;
                        try
                        {
                            switch (file.Extension)
                            {
                                case ".wdb":
                                    reader = new WdbReader(file.FullName);
                                    break;
                                case ".adb":
                                    reader = new AdbReader(file.FullName);
                                    break;
                                case ".db2":
                                    reader = new Db2Reader(file.FullName);
                                    break;
                                case ".dbc":
                                    reader = new DbcReader(file.FullName);
                                    break;
                                default:
                                    continue;
                            }

                            var tableName = file_name;
                            if (element.Attributes["tablename"] != null)
                                tableName = element.Attributes["tablename"].Value;

                            writer.WriteLine(" -- {0} statement", tableName);
                            bool error = false;
                        
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            for (int i = 0; i < reader.RecordsCount; ++i)
                            {
                                RowReader = reader[i];

                                var str_hex = string.Join("", reader.GetRowAsByteArray(i).Select(n => n.ToString("X2")));
                                var str = string.Format("REPLACE INTO {0} VALUES ('{1}'", tableName, reader.Locale);

                                foreach (XmlElement recordInfo in element.ChildNodes.OfType<XmlElement>())
                                {
                                    str += ", ";
                                    ReadType(ref str, recordInfo, ref error, reader.StringTable);
                                }

                                str += ");";

                                if (RowReader.BaseStream.Position != RowReader.BaseStream.Length)
                                    throw new Exception(string.Format("Row Length ({0}) != Row End Position ({1}); difference: {2}",
                                        RowReader.BaseStream.Length, RowReader.BaseStream.Position, RowReader.BaseStream.Length - RowReader.BaseStream.Position));

                                writer.WriteLine(str);

                                if (reader.RecordsCount > 0)
                                {
                                    int perc = i * 100 / reader.RecordsCount;
                                    if ((i % 100) == 0)
                                    {
                                        int cp = Console.CursorTop;
                                        Console.WriteLine("║ {0,-30}║ {1,-8}║ {2,-8}║ {3,-8}║ {4,-8}║", file.Name, reader.Locale, reader.Build, i, perc + "%");
                                        Console.SetCursorPosition(0, cp);
                                    }
                                }
                            }
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            writer.Flush();
                            Console.WriteLine("║ {0,-30}║ {1,-8}║ {2,-8}║ {3,-8}║ {4,-8}║", file.Name, reader.Locale, reader.Build, reader.RecordsCount, (error ? "ERROR" : "OK"));
                        }
                        catch (Exception ex) 
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            var info = string.Format("ERROR: Read: {0}", file.Name);
                            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║ {0, -70}║{1}║ {2, -70}║", info, Environment.NewLine, ex.Message + " " + (ex.InnerException != null ? ex.InnerException.ToString() : ""));
                            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                        }
                    }
                }

                foreach (XmlElement element in xmlstruct.GetElementsByTagName("QueryFinalPack")[0])
                {
                    if (element.Attributes["text"] != null)
                    {
                        writer.WriteLine(element.Attributes["text"].Value);
                    }
                }
            }
        }

        private void ReadType(ref string str, XmlElement elem, ref bool error, Dictionary<int, string> table, bool isNullable = false)
        {
            double maxVal = double.MaxValue;
            double value  = 0;

            if (elem.Attributes["max"] != null)
                maxVal = double.Parse(elem.Attributes["max"].Value);

            string type = elem.Attributes["type"].Value;

            switch (type)
            {
                case "byte":
                    value = isNullable ? 0 : RowReader.ReadByte();
                    str += value;
                    break;
                case "sbyte":
                    value = isNullable ? 0 : RowReader.ReadSByte();
                    str += value;
                    break;
                case "int":
                    value = isNullable ? 0 : RowReader.ReadInt32();
                    str += value;
                    break;
                case "uint":
                    uint uires = isNullable ? 0u : RowReader.ReadUInt32();
                    if (uires > maxVal) error = true;
                    str += uires;
                    break;
                case "float":
                    value = isNullable ? 0f : RowReader.ReadSingle();
                    str += value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "bstring":
                    {
                        var raw_str = isNullable ? string.Empty : RowReader.ReadPascalString12Bit();
                        str += raw_str.EscapeSqlSumbols();
                    } break;
                case "string":
                    {
                        var raw_str = string.Empty;

                        if (table == null)
                            raw_str = isNullable ? string.Empty : RowReader.ReadCString();
                        else
                            raw_str = /*table[*/(isNullable ? 0 : RowReader.ReadInt32()).ToString()/*]*/;

                        str += raw_str.EscapeSqlSumbols();
                    } break;
                case "list":
                    {
                        if (elem.Attributes["maxcount"] == null)
                            throw new NullReferenceException("maxcount");

                        str = str.Remove(str.Length - 2);
                        int elementCount = int.Parse(elem.Attributes["maxcount"].Value);
                        int readElement = elementCount;

                        if (elem.Attributes["counttype"] != null && elem.Attributes["name"] != null)
                        {
                            var counttype = elem.Attributes["counttype"].Value;
                            readElement = isNullable ? 0 : ReadSimpleType(ref RowReader, counttype);
                            str += ", " + readElement;
                        }

                        for (int i = 0; i < elementCount; ++i)
                        {
                            var needisNullabe = (i >= readElement);
                            foreach (XmlElement structElem in elem.ChildNodes.OfType<XmlElement>())
                            {
                                str += ", ";                                
                                ReadType(ref str, structElem, ref error, table, needisNullabe);
                            }
                        }
                    } break;
            }

            if (value > maxVal) 
                error = true;
        }

        private int ReadSimpleType(ref StreamHandler reader, string type)
        {
            switch (type)
            {
                case "byte":  return reader.ReadByte();
                case "short": return reader.ReadInt16();
                case "int":   return reader.ReadInt32();
                default: throw new Exception("bad type " + type);
            }
        }
    }
}