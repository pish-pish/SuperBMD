using System;
using System.Collections.Generic;
using System.IO;
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
            //args = new string[] { "C:\\Users\\User\\Documents\\Modding\\Modding QuickAccess\\MarioKartDD\\Vocaloid\\KagamineRin_Merged_2.bmd" };

            if (args.Length > 0)
            {
                if (args[0] == "help")
                {
                    DisplayHelp();
                    return;
                }

                Model mod = Model.Load(args[0]);

                if (args[0].EndsWith(".bmd") || args[0].EndsWith(".bdl")) 
                {
                    string outFilepath;

                    if (args.Length > 1) 
                    {
                        outFilepath = args[1];
                    }
                    else 
                    {
                        string inDir = Path.GetDirectoryName(args[0]);
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(args[0]);
                        outFilepath = Path.Combine(inDir, fileNameNoExt + ".dae");
                    }

                    mod.ExportAssImp(args[0], outFilepath, "dae", new ExportSettings());
                }
                else {
                    if (args.Length > 1) 
                        mod.ExportBMD(args[1]);
                    else
                        mod.ExportBMD(args[0] + ".bmd");
                } 
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
