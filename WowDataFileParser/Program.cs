using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
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

                    if (definition.Build > 0 && baseReader.Build != definition.Build)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
                        Console.WriteLine("║  ERROR In {0,-60}║", file.Name + " (build " + baseReader.Build + ")");
                        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        continue;
                    }

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
                            var rowReader = new RowReader(buffer, baseReader.StringTable);

                            foreach (var field in fstruct.Fields)
                                rowReader.ReadType(field);

                            lock (writer)
                            {
                                writer.WriteLine("REPLACE INTO `{0}` VALUES (\'{1}\'{2});",
                                    fstruct.TableName, baseReader.Locale, rowReader.ToString());
                            }

                            if (rowReader.Remains > 0)
                                throw new Exception("Remained unread " + rowReader.Remains + " bytes");

                            rowReader.Dispose();

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
    }
}