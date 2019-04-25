using System;
using System.Globalization;
using SuperBMDLib;
using System.Text;
using SuperBMDLib.Materials;
using SuperBMDLib.Geometry.Enums;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

namespace SuperBMDLib
{
    class Program
    {
        static void Main(string[] args)
        {
            // Prevents floats being written to the .dae with commas instead of periods on European systems.
            CultureInfo.CurrentCulture = new CultureInfo("", false);

            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                DisplayHelp();
                return;
            }

            Arguments cmd_args = new Arguments(args);

            List<Material> mat_presets = null; 
            Model mod;
            if (cmd_args.do_profile) {
                if (cmd_args.input_path.EndsWith(".bmd") || cmd_args.input_path.EndsWith(".bdl")) {
                    mod = Model.Load(cmd_args, mat_presets, "");
                    mod.ModelStats.DisplayInfo();
                    return;
                }
                else {
                    Console.WriteLine("Profiling is only supported for BMD/BDL!");
                }

            }

            if (cmd_args.materials_path != "") {
                JsonSerializer serializer = new JsonSerializer();

                serializer.Converters.Add(
                    (new Newtonsoft.Json.Converters.StringEnumConverter())
                );

                using (TextReader file = File.OpenText(cmd_args.materials_path)) {
                    using (JsonTextReader reader = new JsonTextReader(file)) {
                        try {
                            mat_presets = serializer.Deserialize<List<Material>>(reader);
                        }
                        catch (Newtonsoft.Json.JsonReaderException e) {
                            Console.WriteLine(String.Format("Error encountered while reading {0}", cmd_args.materials_path));
                            Console.WriteLine(String.Format("JsonReaderException: {0}", e.Message));
                            return;
                        }
                        catch (Newtonsoft.Json.JsonSerializationException e) {
                            Console.WriteLine(String.Format("Error encountered while reading {0}", cmd_args.materials_path));
                            Console.WriteLine(String.Format("JsonSerializationException: {0}", e.Message));
                            return;
                        }
                    }
                }
            }

            string additionalTexPath = null; 
            if ( cmd_args.materials_path != "") {
                additionalTexPath = Path.GetDirectoryName(cmd_args.materials_path);
            }
            mod = Model.Load(cmd_args, mat_presets, additionalTexPath);

            if (cmd_args.input_path.EndsWith(".bmd") || cmd_args.input_path.EndsWith(".bdl"))
                mod.ExportAssImp(cmd_args.output_path, "dae", new ExportSettings());
            else
                mod.ExportBMD(cmd_args.output_path, cmd_args.output_bdl);
        }

        /// <summary>
        /// Prints credits and argument descriptions to the console.
        /// </summary>
        private static void DisplayHelp()
        {
            Console.WriteLine();
            Console.WriteLine("SuperBMD: A tool to import and export various 3D model formats into the Binary Model (BMD or BDL) format.");
            Console.WriteLine("Written by Sage_of_Mirrors/Gamma (@SageOfMirrors) and Yoshi2/RenolY2.");
            Console.WriteLine("Made possible with help from arookas, LordNed, xDaniel, and many others.");
            Console.WriteLine("Visit https://github.com/Sage-of-Mirrors/SuperBMD/wiki for more information.");
            Console.WriteLine();
            Console.WriteLine("Usage: SuperBMD.exe (inputfilepath) [outputfilepath] [-m/mat filepath]\n" +
                              "       [-x/--texheader filepath] [-t/--tristrip mode] [-r/--rotate] [-b/--bdl]");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("\t                   inputfilepath\tPath to the input file, either a BMD/BDL file or a DAE model.");
            Console.WriteLine("\t                  outputfilepath\tPath to the output file.");
            Console.WriteLine("\t-m/--mat                filePath\tPath to the material presets JSON for DAE to BMD conversion.");
            Console.WriteLine("\t-x/--texheader          filePath\tPath to the texture headers JSON for DAE to BMD conversion.");
            Console.WriteLine("\t-t/--tristrip           mode\t\tMode for tristrip generation.");
            Console.WriteLine("\t\tstatic: Only generate tristrips for static (unrigged) meshes.");
            Console.WriteLine("\t\tall:    Generate tristrips for all meshes.");
            Console.WriteLine("\t\tnone:   Do not generate tristrips.");
            Console.WriteLine();
            Console.WriteLine("\t-r/--rotate\t\t\t\tRotate the model from Z-up to Y-up orientation.");
            Console.WriteLine("\t-b/--bdl\t\t\t\tGenerate a BDL instead of a BMD.");
        }
    }
}
