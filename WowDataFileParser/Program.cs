using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using MS.Internal.Ink;
using WowDataFileParser.Definitions;
using WowDataFileParser.Readers;
/*
    9552 = ═    9553 = ║    9554 = ╒    9555 = ╓    9556 = ╔    9557 = ╕    9558 = ╖    9559 = ╗
  
    9560 = ╘    9561 = ╙    9562 = ╚    9563 = ╛    9564 = ╜    9565 = ╝    9566 = ╞    9567 = ╟
  
    9568 = ╠    9569 = ╡    9570 = ╢    9571 = ╣    9572 = ╤    9573 = ╥    9574 = ╦    9575 = ╧
  
    9576 = ╨    9577 = ╩    9578 = ╪    9579 = ╫    9580 = ╬
 */

namespace WowDataFileParser
{
    class Program
    {
        static readonly string[] FILE_FILTER = { ".wdb", ".adb", ".dbc", ".db2" };
        const string DEF         = "definitions.xml";
        const string OUTPUT_FILE = "output.sql";
        const string VERSION     = "2.2";

        static Definition definition;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (o, ex) => {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║ ERROR: {0,-63}║", (ex.ExceptionObject as Exception).Message);
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
                Console.ForegroundColor = ConsoleColor.Cyan;
            };

            File.Delete(OUTPUT_FILE);

            if (!File.Exists(DEF))
                throw new FileNotFoundException(DEF);

            using (var stream = File.OpenRead(DEF))
            {
                definition = (Definition)new XmlSerializer(typeof(Definition))
                    .Deserialize(stream);
            }

            Console.Title = "WoW data file parser";
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                 Parser wow cached data files v{0, -24}║", VERSION);
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════╦═════════╦═════════╦═════════╦═════════╗");
            Console.WriteLine("║           Name                ║ Locale  ║  Build  ║  Count  ║ Status  ║");
            Console.WriteLine("╠═══════════════════════════════╬═════════╬═════════╬═════════╬═════════╣");

            Parse();

            Console.WriteLine("╚═══════════════════════════════╩═════════╩═════════╩═════════╩═════════╝");
            Console.WriteLine();
            Console.Write("Please, press the \"F5\" to generate a database structure: ");

            return;
            if (Console.ReadKey().Key != ConsoleKey.F5)
                return;

            Console.WriteLine(); Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔═════════════════════════════════════════════════════╗");
            Console.WriteLine("║          Auto generate db structure v{0, -15}║", VERSION);
            Console.WriteLine("╚═════════════════════════════════════════════════════╝");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════╦═════════════════╗");
            Console.WriteLine("║ Element name                      ║      Type       ║");
            Console.WriteLine("╠═══════════════════════════════════╬═════════════════║");

               // new SqlTable();

            Console.WriteLine("╚═══════════════════════════════════╩═════════════════╝");
            Console.ReadKey();
        }

        static void Parse()
        {
            var files = new DirectoryInfo(Environment.CurrentDirectory)
                .GetFiles("*.*", SearchOption.AllDirectories);

            if (files.Length == 0)
                throw new Exception("Folder is Empty");

            using (var writer = new StreamWriter(OUTPUT_FILE, false))
            {
                foreach (var file in files)
                {
                    if (!FILE_FILTER.Contains(file.Extension))
                        continue;

                    var file_name = Path.GetFileNameWithoutExtension(file.Name);
                    BaseReader baseReader = null;
                    switch (file.Extension)
                    {
                        case ".wdb": baseReader = new WdbReader(file.FullName); break;
                        case ".adb": baseReader = new AdbReader(file.FullName); break;
                        case ".db2": baseReader = new Db2Reader(file.FullName); break;
                        case ".dbc": baseReader = new DbcReader(file.FullName); break;
                        default: continue;
                    }

                    FileStruct fstruct = definition.GetStructure(file.Name, baseReader.Build);

                    if (fstruct == null)
                        continue;

                    var ssp = 0;
                    for (int i = 0; i < baseReader.RecordsCount; ++i)
                    {
                        var reader = new BitStreamReader(baseReader[i]);
                        var data   = new TreeData();

                        fstruct.Init();

                        foreach (var field in fstruct.Fields)
                            ReadType(fstruct.Fields, field, ref reader, baseReader.StringTable, data);

                        if (reader.Remains > 0)
                        {
                            throw new Exception();
                        }

                        var sql_text = data.ToSqlString(fstruct.TableName, baseReader.Locale);
                        writer.WriteLine(sql_text);
                        writer.Flush();
                        int perc = i * 100 / baseReader.RecordsCount;
                        if (perc != ssp)
                        {
                            ssp = perc;
                            int cp = Console.CursorTop;
                            Console.WriteLine("║ {0,-30}║ {1,-8}║ {2,-8}║ {3,-8}║ {4,-8}║",
                                file.Name, baseReader.Locale, baseReader.Build, i, perc + "%");
                            Console.SetCursorPosition(0, cp);
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    writer.Flush();
                    Console.WriteLine("║ {0,-30}║ {1,-8}║ {2,-8}║ {3,-8}║ {4,-8}║",
                        file.Name, baseReader.Locale, baseReader.Build, baseReader.RecordsCount, "OK");
                }
            }
        }

        static void ReadType(IList<Field> fstore, Field field, ref BitStreamReader reader, Dictionary<int, string> stringTable, TreeData data, bool read = true)
        {
            var count = field.Size;
            if (count == 0)
                count = fstore.GetValueByName(field.SizeLink);

            Action<object> SetVal = (value) => {
                    field.Value = value;
                    if (!string.IsNullOrWhiteSpace(field.Name))
                        data.Add(value);
            };

            switch (field.Type)
            {
                case DataType.Bool:    SetVal(read ? reader.ReadBit()          : false); break;
                case DataType.Byte:    SetVal(read ? reader.ReadByte(count)    : 0    ); break;
                case DataType.Short:   SetVal(read ? reader.ReadInt16(count)   : 0    ); break;
                case DataType.Ushort:  SetVal(read ? reader.ReadUInt16(count)  : 0    ); break;
                case DataType.Int:     SetVal(read ? reader.ReadInt32(count)   : 0    ); break;
                case DataType.Uint:    SetVal(read ? reader.ReadUInt32(count)  : 0    ); break;
                case DataType.Long:    SetVal(read ? reader.ReadInt64(count)   : 0    ); break;
                case DataType.Ulong:   SetVal(read ? reader.ReadUInt64(count)  : 0    ); break;
                case DataType.Float:   SetVal(read ? reader.ReadFloat()        : 0f   ); break;
                case DataType.Double:  SetVal(read ? reader.ReadDouble()       : 0d   ); break;
                case DataType.Pstring: SetVal(read ? reader.ReadPString(count) :null  ); break;
                case DataType.String2: SetVal(read ? reader.ReadString3(count) :null  ); break;
                case DataType.String:
                    {
                        if (stringTable != null)
                        {
                            var offset = reader.ReadInt32();
                            SetVal(stringTable[offset]);
                        }
                        else if (read)
                        {
                            if (count == 0 && field.SizeLink == null)
                                SetVal(reader.ReadCString());
                            else
                            {
                                if (count == 1024)
                                    SetVal(reader.ReadString2(count));
                                else
                                    SetVal(reader.ReadString2(count));
                            }
                        }
                        else
                        {
                            SetVal(null);
                        }
                    } break;
                case DataType.List:
                    {
                        var size = 0;
                        if (field.Size > 0)
                        {
                            size = read ? reader.ReadSize(field.Size) : 0;
                            SetVal(size);
                        }
                        else if (!string.IsNullOrWhiteSpace(field.SizeLink))
                        {
                            size = fstore.GetValueByName(field.SizeLink);
                        }
                        else if (field.Maxsize > 0)
                        {
                            size = field.Maxsize;
                        }

                        var subdata = data.Alloc();
                        for (int i = 0; i < field.Maxsize; ++i)
                        {
                            read = i < size;
                            foreach (var subfield in field.Fields)
                            {
                                ReadType(field.Fields, subfield, ref reader, stringTable, subdata, read);
                            }
                        }
                    } break;
                default: break;
            }
        }
    }
}