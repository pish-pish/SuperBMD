using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperBMD;

namespace SuperBMD_UnitTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "help")
                {
                    DisplayHelp();
                    return;
                }

                Model mod = Model.Load(args[0]);

                if (args[0].EndsWith(".bmd") || args[0].EndsWith(".bdl"))
                    mod.ExportAssImp(args[0], "dae", new ExportSettings());
                else
                    mod.ExportBMD(args[0] + ".bmd");
            }
            else
                DisplayHelp();
        }

        private static void DisplayHelp()
        {
            Console.WriteLine();
            Console.WriteLine("SuperBMD: A tool to import and export various 3D model formats into the Binary Model (BMD) format.");
            Console.WriteLine("Written by Sage_of_Mirrors/Gamma (@SageOfMirrors).");
            Console.WriteLine("Made possible with help from arookas, LordNed, xDaniel, and many others.");
            Console.WriteLine("Visit https://github.com/Sage-of-Mirrors/SuperBMD/wiki for more information.");
            Console.WriteLine("This is an inofficial version with a Triangle Strip algorithm added from BrawlLib. Report any issues to Yoshi2#6013 (RenolY2 on github).");
        }
    }
}
