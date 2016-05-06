using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using WowDataFileParser.Definitions;
using WowDataFileParser.Readers;

namespace WowDataFileParser
{
    class Program
    {
        static string outputPath = "output.sql";
        static string definitionPath = "definitions.xml";
        static Definition definition;
        public static bool DebugOutput = false;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                DebugOutput = args.Any(n => n.ToLower() == "/debug");

                foreach (var arg in args)
                {
                    if (arg.IndexOf("/p") != -1)
                        definitionPath = arg.Substring(2);
                }
            }

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
                    Console.WriteLine($"Unknown attribute: '{e.Attr.Name}' at line: {e.LineNumber} position: {e.LinePosition}");
                };

                serialiser.UnknownElement += (o, e) => {
                    Console.WriteLine($"Unknown Element: '{e.Element.Name}' at line: {e.LineNumber} position: {e.LinePosition}");
                };

                definition = (Definition)serialiser.Deserialize(stream);
            }

            if (definition.Build > 0)
                outputPath = string.Format($"output_{definition.Build}.sql");

            File.Delete(outputPath);

            var version = Assembly.GetExecutingAssembly().GetName().Version;

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           Parser wow cached data files v{0}.{1} for build {2,-6}          ║", version.Major, version.Minor, definition.Build);
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
                    var fstruct = definition.Files.Where(
                        n => Regex.IsMatch(file.Name, n.Name, RegexOptions.IgnoreCase))
                        .FirstOrDefault();

                    if (fstruct == null)
                        continue;

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

                    var storedProgress = 0;
                    var progress = 0;
                    var cursorPosition = Console.CursorTop;

                    writer.WriteLine($"-- {file.Name} statements");

                    stopwatch.Reset();
                    stopwatch.Start();

                    try
                    {
                        Parallel.ForEach(baseReader.Rows, buffer =>
                        {
                            using (var rowReader = new RowReader(buffer, baseReader.StringTable))
                            {
                                var content = new StringBuilder();
                                foreach (var field in fstruct.Fields)
                                {
                                    if (field.Type == DataType.TableList)
                                    {
                                        var size = 0;
                                        if (field.Size > 0)
                                            size = rowReader.ReadSize(field.Size);
                                        if (!string.IsNullOrWhiteSpace(field.SizeLink))
                                            size = rowReader.GetValueByFiedName(field.SizeLink);
                                        else if (field.Maxsize > 0)
                                            size = field.Maxsize;

                                        var entry = rowReader.GetValueByFiedName(field.KeyFieldName);

                                        if (entry < 1)
                                            throw new Exception();

                                        for (int i = 0; i < size; ++i)
                                        {
                                            var subContent = new StringBuilder();

                                            foreach (var subfield in field.Fields)
                                            {
                                                rowReader.ReadField(ref subContent, subfield);
                                            }

                                            lock (writer)
                                            {
                                                writer.WriteLine($"REPLACE INTO `{field.Name}` VALUES (\'{baseReader.Locale}\', {entry}, {i + 1}{subContent});");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        rowReader.ReadField(ref content, field);
                                    }
                                }

                                lock (writer) {
                                    writer.WriteLine($"REPLACE INTO `{fstruct.Table}` VALUES (\'{baseReader.Locale}\'{content});");
                                }

                                if (rowReader.Remains > 0)
                                    throw new Exception($"Remained unread {rowReader.Remains} bytes");
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