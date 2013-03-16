using System;
using System.IO;
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
        const string VERSION = "2.0";

        static void Main(string[] args)
        {
            Console.Title = "WoW data file parser";
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                 Parser wow cached data files v{0, -24}║", VERSION);
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════╦═════════╦═════════╦═════════╦═════════╗");
            Console.WriteLine("║           Name                ║ Locale  ║  Build  ║  Count  ║ Status  ║");
            Console.WriteLine("╠═══════════════════════════════╬═════════╬═════════╬═════════╬═════════╣");
            try
            {
                new Parser();
            }
            catch (Exception ex)
            {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║ ERROR: {0,-63}║", ex.Message);
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.ForegroundColor = ConsoleColor.Cyan;
            }
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
            try
            {
                new SqlTable();
            }
            catch (Exception ex)
            {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔═════════════════════════════════════════════════════╗");
            Console.WriteLine("║ ERROR: {0,-45}║", ex.Message);
            Console.WriteLine("╚═════════════════════════════════════════════════════╝");
            Console.ForegroundColor = ConsoleColor.Cyan;
            }
            Console.WriteLine("╚═══════════════════════════════════╩═════════════════╝");
            Console.ReadKey();
        }
    }
}