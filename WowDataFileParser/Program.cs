using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
        static string outputPath = "output.sql";
        static string definitionPath = "definitions.xml";
        static Definition definition;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
                definitionPath = args[0];

            Console.Title = "WoW data file parser";

            AppDomain.CurrentDomain.UnhandledException += (o, ex) => {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║ ERROR: {0,-63}║", (ex.ExceptionObject as Exception).Message);
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
                Console.ForegroundColor = ConsoleColor.Cyan;
            };

            if (!File.Exists(definitionPath))
                throw new FileNotFoundException("File not found", definitionPath);

            Console.ForegroundColor = ConsoleColor.Red;
            using (var stream = File.OpenRead(definitionPath))
            {
                var serialiser = new XmlSerializer(typeof(Definition));

                serialiser.UnknownAttribute += (o, e) => {
                    Console.WriteLine("Unknown attribute: '{0}' at line: {1} position: {2}",
                        e.Attr.Name, e.LineNumber, e.LinePosition);
                };

                serialiser.UnknownElement += (o, e) => {
                    Console.WriteLine("Unknown Element: '{0}' at line: {1} position: {2}",
                        e.Element.Name, e.LineNumber, e.LinePosition);
                };

                definition = (Definition)serialiser.Deserialize(stream);
            }

            if (definition.Build > 0)
                outputPath = string.Format("output_{0}.sql", definition.Build);

            File.Delete(outputPath);

            var mj = Assembly.GetExecutingAssembly().GetName().Version.Major;
            var mi = Assembly.GetExecutingAssembly().GetName().Version.Minor;

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           Parser wow cached data files v{0}.{1} for build {2,-6}          ║", mj, mi, definition.Build);
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════╦═════════╦═════════╦═════════╦═════════╗");
            Console.WriteLine("║           Name                ║ Locale  ║  Build  ║  Count  ║ Status  ║");
            Console.WriteLine("╠═══════════════════════════════╬═════════╬═════════╬═════════╬═════════╣");

            Program.Parse();

            Console.WriteLine("╚═══════════════════════════════╩═════════╩═════════╩═════════╩═════════╝");
            Console.ReadKey();
        }

        static void Parse()
        {
            var directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
            var files = directoryInfo.GetFiles("*.wdb", SearchOption.AllDirectories)
                .Concat(directoryInfo.GetFiles("*.adb", SearchOption.AllDirectories))
                .Concat(directoryInfo.GetFiles("*.db2", SearchOption.AllDirectories))
                ;

            var stopwatch = new Stopwatch();

            using (var writer = new StreamWriter(outputPath, false))
            {
                writer.AutoFlush = true;

                // Write structure DB
                SqlTable.CreateStructure(writer, definition);

                foreach (var file in files)
                {
                    BaseReader baseReader = null;
                    switch (file.Extension)
                    {
                        case ".wdb": baseReader = new WdbReader(file.FullName); break;
                        case ".adb": baseReader = new AdbReader(file.FullName); break;
                        case ".db2": baseReader = new Db2Reader(file.FullName); break;
                        default: throw new NotImplementedException();
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

                    writer.WriteLine("-- {0} statements", file.Name);

                    stopwatch.Reset();
                    stopwatch.Start();

                    var tableName = fstruct.TableName;

                    try
                    {
                        Parallel.ForEach(baseReader.Rows, buffer =>
                        {
                            using (var rowReader = new RowReader(buffer, baseReader.StringTable))
                            {
                                foreach (var field in fstruct.Fields)
                                    rowReader.ReadType(field);

                                lock (writer) {
                                    writer.WriteLine("REPLACE INTO `{0}` VALUES (\'{1}\'{2});",
                                        tableName, baseReader.Locale, rowReader.ToString());
                                }

                                if (rowReader.Remains > 0)
                                    throw new Exception("Remained unread " + rowReader.Remains + " bytes");
                            }

                            Interlocked.Increment(ref progress);

                            int perc = progress * 100 / baseReader.RecordsCount;
                            if (perc != storedProgress)
                            {
                                storedProgress = perc;
                                Console.WriteLine("║ {0,-30}║ {1,-8}║ {2,-8}║ {3,-8}║ {4,-8}║",
                                    file.Name, baseReader.Locale, baseReader.Build, progress, perc + "%");
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
                    finally
                    {
                        stopwatch.Stop();
                        writer.WriteLine();
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
}