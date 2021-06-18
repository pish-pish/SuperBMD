using Newtonsoft.Json;
using SuperBMDLib.Materials;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace SuperBMDLib
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "SuperBMD Console";
            Assembly thisAssem = typeof(Program).Assembly;
            AssemblyName thisAssemName = thisAssem.GetName();
            Version ver = thisAssemName.Version;
            Console.WriteLine("SuperBMD v" + ver);
            Console.WriteLine();
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
                if (cmd_args.input_path.EndsWith(".bmd") || cmd_args.input_path.EndsWith(".bdl"))
                {
                    Console.WriteLine("Reading the model...");
                    mod = Model.Load(cmd_args, mat_presets, "");
                    Console.WriteLine();
                    Console.WriteLine("Profiling ->");
                    mod.ModelStats.DisplayInfo();
                    mod.ModelStats.DisplayModelInfo(mod); Console.WriteLine();
                    Console.WriteLine("Press any key to Exit");
                    Console.ReadKey();
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
                Console.WriteLine("Reading the Materials...");
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
            if (cmd_args.materials_path != "") {
                additionalTexPath = Path.GetDirectoryName(cmd_args.materials_path);
            }
            FileInfo fi = new FileInfo(cmd_args.input_path);
            string destinationFormat = (fi.Extension == ".bmd" || fi.Extension == ".bdl") ? ".DAE" : (cmd_args.output_bdl ? ".BDL" : ".BMD");
            
            if (destinationFormat == ".DAE" && cmd_args.export_obj) {
                destinationFormat = ".OBJ";
            }

            Console.WriteLine(string.Format("Preparing to convert {0} from {1} to {2}", fi.Name.Replace(fi.Extension, ""), fi.Extension.ToUpper(), destinationFormat));
            mod = Model.Load(cmd_args, mat_presets, additionalTexPath);

            if (cmd_args.hierarchyPath != "") {
                mod.Scenegraph.LoadHierarchyFromJson(cmd_args.hierarchyPath);
            }

            if (cmd_args.input_path.EndsWith(".bmd") || cmd_args.input_path.EndsWith(".bdl"))
            {
                Console.WriteLine(string.Format("Converting {0} into {1}...", fi.Extension.ToUpper(), destinationFormat));
                if (cmd_args.export_obj) {
                    mod.ExportAssImp(cmd_args.output_path, "obj", new ExportSettings(), cmd_args);
                }
                else {
                    mod.ExportAssImp(cmd_args.output_path, "dae", new ExportSettings(), cmd_args);
                }
                
                
            }
            else
            {
                Console.Write("Finishing the Job...");
                mod.ExportBMD(cmd_args.output_path, cmd_args.output_bdl);
                Console.WriteLine("✓");
            }

            Console.WriteLine();
            Console.WriteLine("The Conversion is complete!");
            Console.WriteLine();
        }

        /// <summary>
        /// Prints credits and argument descriptions to the console.
        /// </summary>
        private static void DisplayHelp()
        {
            Console.WriteLine();
            Console.WriteLine("SuperBMD: A tool to import and export various 3D model formats into the Binary Model (BMD or BDL) format.");
            Console.WriteLine("Written by Sage_of_Mirrors/Gamma (@SageOfMirrors) and Yoshi2/RenolY2.");
            Console.WriteLine("Console lines written by Super Hackio");
            Console.WriteLine("Made possible with help from arookas, LordNed, xDaniel, and many others.");
            Console.WriteLine("Visit https://github.com/Sage-of-Mirrors/SuperBMD/wiki for more information.");
            Console.WriteLine();
            Console.WriteLine("Usage: SuperBMD.exe (inputfilepath) [outputfilepath] [-m/mat filepath]\n" +
                              "       [-x/--texheader filepath] [-t/--tristrip mode] [-r/--rotate] [-b/--bdl]");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("\t                   inputfilepath\tPath to the input file, either a BMD/BDL file or a DAE model.");
            Console.WriteLine("\t                  outputfilepath\tPath to the output file.");
            Console.WriteLine("\t-m/--mat                filePath\tPath to the material configuration JSON for DAE to BMD conversion.");
            Console.WriteLine("\t-m/--outmat             filePath\tOutput path for the material configuration JSON for BMD to DAE conversion.");
            Console.WriteLine("\t-x/--texheader          filePath\tPath to the texture headers JSON for DAE to BMD conversion.");
            Console.WriteLine("\t-t/--tristrip           mode\t\tMode for tristrip generation.");
            Console.WriteLine("\t\tstatic: Only generate tristrips for static (unrigged) meshes.");
            Console.WriteLine("\t\tall:    Generate tristrips for all meshes.");
            Console.WriteLine("\t\tnone:   Do not generate tristrips.");
            Console.WriteLine();
            Console.WriteLine("\t-r/--rotate\t\t\t\tRotate the model from Z-up to Y-up orientation.");
            Console.WriteLine("\t-b/--bdl\t\t\t\tGenerate a BDL instead of a BMD.");
            Console.WriteLine("\t-b/--nosort\t\t\t\tDisable naturalistic sorting of meshes by name.");
            Console.WriteLine("\t-b/--onematpermesh\t\t\tEnsure one material per mesh.");
            Console.WriteLine("\t-b/--exportobj\t\t\t\tIf input is BMD/BDL, export the model as Wavefront OBJ instead of Collada (.DAE).");
            Console.WriteLine("\t-b/--texfloat32\t\t\t\tOn conversion into BMD, always store texture UV coordinates as 32 bit floats.");
            Console.WriteLine("\t-b/--degeneratetri\t\t\tOn conversion into BMD, write triangle lists as triangle strips using degenerate triangles.");
            Console.WriteLine();
            Console.WriteLine("\t-b/--profile\t\t\t\tGenerate a report with information on the .BMD/.BDL (Other formats not supported)");
            Console.WriteLine();
            Console.WriteLine("\t-a/--animation\t\t\t\tGenerate *.bck files from animation data stored in DAE, if present");
            Console.WriteLine();
        }
    }
}
