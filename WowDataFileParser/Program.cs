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
        const string DEF         = "definitions.xml";
        const string OUTPUT_FILE = "output.sql";
        const string VERSION     = "4.1";

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

            if (files.Length == 0)
                throw new Exception("Folder is Empty");

            using (var writer = new StreamWriter(OUTPUT_FILE, false))
            {
                writer.AutoFlush = false;

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
                        //case ".dbc": baseReader = new DbcReader(file.FullName); break;
                        default: continue;
                    }

                    var fstruct = definition[file.Name, baseReader.Build];

                    if (fstruct == null)
                        continue;

                    var ssp = 0;
                    var progress = 0;
                    var tname = fstruct.Build > 0 ? string.Format("{0}_{1}", fstruct.TableName, fstruct.Build) : fstruct.TableName;
                    int cp = Console.CursorTop;

                    var sw = new Stopwatch();
                    sw.Start();

                    var cache = new Dictionary<int, FileStruct>();
                    Parallel.ForEach(baseReader.Rows, buffer => {

                        var reader = new BitStreamReader(buffer);
                        var insert = new StringBuilder();
                        var valList = new Dictionary<string, IConvertible>();

                        foreach (var field in fstruct.Fields)
                            ReadType(field, reader, baseReader.StringTable, insert, valList, true);

                        lock (writer)
                            writer.WriteLine("REPLACE INTO `{0}` VALUES (\'{1}\' {2});", tname, baseReader.Locale, insert.ToString());

                        if (reader.Remains > 0)
                            throw new Exception("reader.Remains = " + reader.Remains);

                        reader.Dispose();

                        Interlocked.Increment(ref progress);
                        int perc = progress * 100 / baseReader.RecordsCount;
                        if (perc != ssp)
                        {
                            ssp = perc;
                            Console.WriteLine("║ {0,-30}║ {1,-8}║ {2,-8}║ {3,-8}║ {4,-8}║", file.Name, baseReader.Locale, baseReader.Build, progress, perc + "%");
                            Console.SetCursorPosition(0, cp);
                        }
                    });

                    sw.Stop();
                    GC.Collect();

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    writer.Flush();
                    Console.SetCursorPosition(0, cp);
                    Console.WriteLine("║ {0,-30}║ {1,-8}║ {2,-8}║ {3,-8}║ {4,-8}║",
                        file.Name, baseReader.Locale, baseReader.Build, baseReader.RecordsCount,
                        sw.Elapsed.TotalSeconds.ToString("f", CultureInfo.InvariantCulture) + "sec");

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