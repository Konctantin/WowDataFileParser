using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using WDReader.Reader;

namespace WowDataFileParser
{
    internal class Parser
    {
        public static readonly string[] FILE_FILTER = { ".wdb", ".adb", ".dbc", ".db2" };
        public static readonly string DEF           = "definitions.xml";
        public static readonly string OUTPUT_FILE   = "output.sql";

        private BitStreamReader RowReader;
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
                                RowReader = new BitStreamReader( reader[i] );

                                var str_hex = string.Join("", reader.GetRowAsByteArray(i).Select(n => n.ToString("X2")));
                                var str = string.Format("REPLACE INTO {0} VALUES ('{1}'", tableName, reader.Locale);

                                foreach (var recordInfo in element.ChildNodes.OfType<XmlElement>())
                                {
                                    str += ", ";
                                    ReadType(ref str, recordInfo, ref error, reader.StringTable);
                                }

                                str += ");";

                                //if (RowReader.BaseStream.Position != RowReader.BaseStream.Length)
                                //    throw new Exception(string.Format("Row Length ({0}) != Row End Position ({1}); difference: {2}",
                                //        RowReader.BaseStream.Length, RowReader.BaseStream.Position, RowReader.BaseStream.Length - RowReader.BaseStream.Position));

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
                            var exMessage = ex.Message + " " + (ex.InnerException != null ? ex.InnerException.ToString() : "");
                            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
                            Console.WriteLine("║ {0, -70}║{1}║ {2, -70}║", info, Environment.NewLine, exMessage);
                            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                        }
                    }
                }
            }
        }

        private void ReadType(ref string str, XmlElement elem, ref bool error, Dictionary<int, string> table, bool isNullable = false)
        {
            var maxVal   = double.MaxValue;
            var value    = 0d;
            var bitcount = 0;

            if (elem.Attributes["max"] != null)
                maxVal = double.Parse(elem.Attributes["max"].Value);

            if (elem.Attributes["bit"] != null)
                bitcount = int.Parse(elem.Attributes["bit"].Value);


            var type = elem.Attributes["type"].Value;

            switch (type)
            {
                case "byte":
                    {
                        if (isNullable)
                            value = 0;
                        else
                        {
                            value = bitcount == 0 
                                ? RowReader.ReadByte() 
                                : (int)RowReader.ReadUInt16Reverse(bitcount);
                        }
                        str += value;
                    }
                    break;
                case "short":
                    {
                        if (isNullable)
                            value = 0;
                        else
                        {
                            value = bitcount == 0
                                ? RowReader.ReadInt32()
                                : (int)RowReader.ReadUInt16Reverse(bitcount);
                        }
                        str += value;
                    }
                    break;
                case "int":
                    {
                        if (isNullable)
                            value = 0;
                        else
                        {
                            value = bitcount == 0
                                ? RowReader.ReadInt32()
                                : (int)RowReader.ReadUInt32Reverse(bitcount);
                        }
                        str += value;
                    }
                    break;
                case "uint":
                    {
                        if (isNullable)
                            value = 0u;
                        else
                        {
                            value = bitcount == 0
                                ? RowReader.ReadUInt32Reverse(32)
                                : RowReader.ReadUInt32Reverse(bitcount);
                        }
                        if (value > maxVal)
                            error = true;
                        str += value;
                    }
                    break;
                case "float":
                    value = isNullable ? 0f : RowReader.ReadFloat();
                    str += value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "pstring":
                    {
                        var raw_str = string.Empty;
                        if (!isNullable)
                        {
                            int count = (int)RowReader.ReadUInt32Reverse(bitcount);
                            raw_str = RowReader.ReadPascalString(count);
                        }
                        str += raw_str.EscapeSqlSumbols();
                    } break;
                case "string":
                    {
                        var raw_str = string.Empty;

                        if (table == null)
                        {
                            if (bitcount > 0)
                            {
                                int count = (int)RowReader.ReadUInt32Reverse(bitcount);
                                raw_str = isNullable
                                    ? string.Empty
                                    : RowReader.ReadPascalString(count);
                            }
                            else
                            {
                                raw_str = isNullable 
                                    ? string.Empty
                                    : RowReader.ReadCString();
                            }
                        }
                        else
                        {
                            var offset = isNullable ? 0 : RowReader.ReadInt32();
                            raw_str = table.ContainsKey(offset) ? table[offset] : string.Empty;
                        }
                        str += raw_str.EscapeSqlSumbols();
                    } break;
                case "list":
                    {
                        if (elem.Attributes["maxcount"] == null)
                            throw new NullReferenceException("maxcount");

                        str = str.Remove(str.Length - 2);
                        var elementCount = int.Parse(elem.Attributes["maxcount"].Value);
                        var readElement = elementCount;

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
                case "stringlist":
                    {
                        List<int> counts = new List<int>();
                        foreach (XmlElement listElement in elem.ChildNodes.OfType<XmlElement>())
                        {
                            var bitCount = int.Parse(listElement.Attributes["bit"].Value);
                            if (listElement.Attributes["reverse"] != null && listElement.Attributes["reverse"].Value == "true")
                                counts.Add((int)RowReader.ReadUInt32Reverse(bitCount));
                            else
                                counts.Add((int)RowReader.ReadUInt32(bitCount));
                        }
                        for (int i = 0; i < counts.Count; ++i)
                        {
                            var sstr = string.Empty;
                            if (RowReader.RemainigLength >= counts[i])
                                sstr = RowReader.ReadString(counts[i]).EscapeSqlSumbols();

                            if (i < counts.Count - 1)
                                sstr += ", ";
                            str += sstr;
                        }
                    }break;
            }

            if (value > maxVal) 
                error = true;
        }

        private int ReadSimpleType(ref BitStreamReader reader, string type)
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