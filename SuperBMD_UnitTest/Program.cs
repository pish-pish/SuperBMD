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
            //args = new string[] { "./orima1.bmd" };
            string in_file = "";
            string out_file = "";
            string mat_file = "";

            int processed_args = 0;

            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "--mat") {
                    mat_file = args[i + 1];
                    i++;
                }
                else if (args[i] == "help") {
                    DisplayHelp();
                    return;
                }
                else {
                    if (processed_args == 0) {
                        in_file = args[i];
                    }
                    else if (processed_args == 1) {
                        out_file = args[i];
                    }

                    processed_args++;
                }
            }

            if (in_file != "")
            {
                Model mod = Model.Load(in_file);

                if (in_file.EndsWith(".bmd") || in_file.EndsWith(".bdl")) 
                {
                    string outFilepath;

                    if (out_file != "") 
                    {
                        outFilepath = out_file;
                    }
                    else 
                    {
                        string inDir = Path.GetDirectoryName(in_file);
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(in_file);
                        outFilepath = Path.Combine(inDir, fileNameNoExt + ".dae");
                    }

                    mod.ExportAssImp(in_file, outFilepath, "dae", new ExportSettings(), mat_file);
                    if (mat_file != "") {
                        using (TextWriter file = File.CreateText(mat_file)) {
                            mod.Materials.DumpJson(file);
                        }
                    }
                }
                else {
                    if (out_file != "") {
                        mod.ExportBMD(out_file, mat_file);
                    }
                    else {
                        mod.ExportBMD(in_file + ".bmd", mat_file);
                    }

                    
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
