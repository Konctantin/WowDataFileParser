using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        const string DEFINITIONS = "definitions.xml";
        const string OUTPUT_FILE = "output.sql";
        const string VERSION     = "4.1";

        static Definition definition;

        [STAThread]
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

            if (!File.Exists(DEFINITIONS))
                throw new FileNotFoundException("File not found", DEFINITIONS);

            using (var stream = File.OpenRead(DEFINITIONS))
            {
                definition = (Definition)new XmlSerializer(typeof(Definition))
                    .Deserialize(stream);
            }

            Console.Title = "WoW data file parser";
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║            Parser wow cached data files v{0} for build {1}          ║", VERSION, definition.Build);
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════╦═════════╦═════════╦═════════╦═════════╗");
            Console.WriteLine("║           Name                ║ Locale  ║  Build  ║  Count  ║ Status  ║");
            Console.WriteLine("╠═══════════════════════════════╬═════════╬═════════╬═════════╬═════════╣");

            Parse();

            Console.WriteLine("╚═══════════════════════════════╩═════════╩═════════╩═════════╩═════════╝");
            Console.WriteLine();
            Console.Write("Please, press the \"F5\" to generate a database structure: ");

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

            SqlTable.CreateSqlTable(definition);

            Console.WriteLine("╚═══════════════════════════════════╩═════════════════╝");
            Console.ReadKey();
        }

        static void Parse()
        {
            var files = new DirectoryInfo(Environment.CurrentDirectory)
                .GetFiles("*.*", SearchOption.AllDirectories);

            var stopwatch = new Stopwatch();

            using (var writer = new StreamWriter(OUTPUT_FILE, false))
            {
                writer.AutoFlush = true;

                foreach (var file in files)
                {
                    if (!FILE_FILTER.Contains(file.Extension))
                        continue;

                    BaseReader baseReader = null;
                    switch (file.Extension)
                    {
                        case ".wdb": baseReader = new WdbReader(file.FullName); break;
                        case ".adb": baseReader = new AdbReader(file.FullName); break;
                        case ".db2": baseReader = new Db2Reader(file.FullName); break;
                        default: continue;
                    }

                    //if (baseReader.Build != definition.Build)
                    //{
                    //    Console.ForegroundColor = ConsoleColor.Red;
                    //    Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
                    //    Console.WriteLine("║  ERROR In {0,-60}║", file.Name + " (build " + baseReader.Build + ")");
                    //    Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
                    //    Console.ForegroundColor = ConsoleColor.Cyan;
                    //    continue;
                    //}

                    var fstruct = definition[file.Name];

                    if (fstruct == null)
                        continue;

                    var storedProgress = 0;
                    var progress = 0;
                    var cursorPosition = Console.CursorTop;

                    stopwatch.Reset();
                    stopwatch.Start();

                    try
                    {
                        Parallel.ForEach(baseReader.Rows, buffer =>
                        {
                            var reader = new BitStreamReader(buffer);
                            var insert = new StringBuilder();
                            var valList = new Dictionary<string, IConvertible>();

                            foreach (var field in fstruct.Fields)
                                ReadType(field, reader, baseReader.StringTable, insert, valList, true);

                            lock (writer)
                            {
                                writer.WriteLine("REPLACE INTO `{0}` VALUES (\'{1}\'{2});",
                                    fstruct.TableName, baseReader.Locale, insert.ToString());
                            }

                            if (reader.Remains > 0)
                                throw new Exception("Remained unread " + reader.Remains + " bytes");

                            reader.Dispose();

                            Interlocked.Increment(ref progress);
                            int perc = progress * 100 / baseReader.RecordsCount;
                            if (perc != storedProgress)
                            {
                                storedProgress = perc;
                                Console.WriteLine("║ {0,-30}║ {1,-8}║ {2,-8}║ {3,-8}║ {4,-8}║", file.Name, baseReader.Locale, baseReader.Build, progress, perc + "%");
                                Console.SetCursorPosition(0, cursorPosition);
                            }
                        });
                    }
                    catch (AggregateException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
                        Console.WriteLine("║  ERROR In {0,-60}║", file.Name + " (build " + baseReader.Build + ")");
                        Console.WriteLine(ex.InnerException.Message);
                        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        cursorPosition = Console.CursorTop;
                    }

                    stopwatch.Stop();
                    writer.Flush();

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.SetCursorPosition(0, cursorPosition);
                    Console.WriteLine("║ {0,-30}║ {1,-8}║ {2,-8}║ {3,-8}║ {4,-8}║",
                        file.Name, baseReader.Locale, baseReader.Build, baseReader.RecordsCount,
                        stopwatch.Elapsed.TotalSeconds.ToString("F", CultureInfo.InvariantCulture) + "sec");

                    baseReader.Dispose();
                }
            }
        }

        static void ReadType(Field field, BitStreamReader reader, Dictionary<int, string> stringTable, StringBuilder content, Dictionary<string, IConvertible> valList, bool read = true)
        {
            Action<IConvertible> SetVal = (value) => {
                if (!string.IsNullOrWhiteSpace(field.Name))
                    valList[field.Name] = value;

                if (value == null)
                    content.Append(", NULL");
                else if (!(value is string))
                    content.Append(", " + value.ToString(CultureInfo.InvariantCulture));
                else
                {
                    var val = value.ToString(CultureInfo.InvariantCulture);
                    if (string.IsNullOrWhiteSpace(val))
                        content.Append(", NULL");
                    else
                        content.Append(", \"" + val.Replace(@"'", @"\'").Replace("\"", "\\\"") + "\"");
                }
            };

            var count = field.Size;
            if (count == 0)
                count = valList.GetValueByName(field.SizeLink);

            switch (field.Type)
            {
                case DataType.Bool:    SetVal(read ? reader.ReadBit()          : 0  ); break;
                case DataType.Byte:    SetVal(read ? reader.ReadByte(count)    : 0  ); break;
                case DataType.Short:   SetVal(read ? reader.ReadInt16(count)   : 0  ); break;
                case DataType.Ushort:  SetVal(read ? reader.ReadUInt16(count)  : 0  ); break;
                case DataType.Int:     SetVal(read ? reader.ReadInt32(count)   : 0  ); break;
                case DataType.Uint:    SetVal(read ? reader.ReadUInt32(count)  : 0  ); break;
                case DataType.Long:    SetVal(read ? reader.ReadInt64(count)   : 0  ); break;
                case DataType.Ulong:   SetVal(read ? reader.ReadUInt64(count)  : 0  ); break;
                case DataType.Float:   SetVal(read ? reader.ReadFloat()        : 0f ); break;
                case DataType.Double:  SetVal(read ? reader.ReadDouble()       : 0d ); break;
                case DataType.Pstring: SetVal(read ? reader.ReadPString(count) :null); break;
                case DataType.String2: SetVal(read ? reader.ReadString3(count) :null); break;
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
                                SetVal(reader.ReadString2(count));
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
                            size = valList.GetValueByName(field.SizeLink);
                        }
                        else if (field.Maxsize > 0)
                        {
                            size = field.Maxsize;
                        }

                        for (int i = 0; i < field.Maxsize; ++i)
                        {
                            read = i < size;
                            foreach (var subfield in field.Fields)
                            {
                                ReadType(subfield, reader, stringTable, content, valList, read);
                            }
                        }
                    } break;
                default:
                    break;
            }
        }
    }
}