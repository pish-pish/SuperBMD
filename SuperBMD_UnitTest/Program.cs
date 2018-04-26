using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SuperBMD;
using SuperBMD.Materials;
using SuperBMD.Geometry.Enums;

namespace SuperBMD_UnitTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string in_file = "";
            string out_file = "";
            string mat_file = "";
            string texheader_file = "";

            bool flipyz = false;
            bool fixNormals = true; // For fixing shading on rigged models

            TristripOption triopt = TristripOption.DoTriStripStatic;

            int processed_args = 0;

            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "--mat") {
                    mat_file = args[i + 1];
                    i++;
                }

                else if (args[i] == "--texheader") {
                    texheader_file = args[i + 1];
                    i++;
                }

                else if (args[i] == "--tristrip") {
                    string opt = args[i + 1];
                    if (opt == "all") {
                        triopt = TristripOption.DoTriStripAll;
                    }
                    else if (opt == "static") {
                        triopt = TristripOption.DoTriStripStatic;
                    }
                    else if (opt == "none") {
                        triopt = TristripOption.DoNotTriStrip;
                    }
                    i++;
                }

                else if (args[i] == "--rotate") {
                    flipyz = true;
                }

                else if (args[i] == "--dontFix") {
                    fixNormals = false;
                }

                else if (args[i] == "--help") {
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
                if ((in_file.EndsWith(".bmd") || in_file.EndsWith(".bdl")) && !out_file.EndsWith(".bmd")) 
                {
                    // BMD -> DAE conversion
                    Model mod = Model.Load(in_file);

                    string outFilepath;

                    if (out_file != "") {
                        outFilepath = out_file;
                        string outDir = Path.GetDirectoryName(out_file);
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(outFilepath);

                        if (mat_file == "") {
                            mat_file = Path.Combine(outDir, fileNameNoExt + "_mat.json");
                        }
                    }
                    else {
                        string inDir = Path.GetDirectoryName(in_file);
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(in_file);
                        outFilepath = Path.Combine(inDir, fileNameNoExt + ".dae");

                        if (mat_file == "") {
                            mat_file = Path.Combine(inDir, fileNameNoExt + "_mat.json");
                        }

                        if (texheader_file == "") {
                            texheader_file = Path.Combine(inDir, fileNameNoExt + "_texheader.json");
                        }
                    }

                    mod.ExportAssImp(in_file, outFilepath, "dae", new ExportSettings());

                    if (mat_file != "") {
                        using (TextWriter file = File.CreateText(mat_file)) {
                            mod.Materials.DumpJson(file);
                        }
                    }

                    if (texheader_file != "") {
                        using (TextWriter file = File.CreateText(texheader_file)) {
                            mod.Textures.DumpTextureHeaders(file);
                        }
                    }
                }

                // Any model -> BMD or BMD -> BMD
                else {
                    List<Material> mat_presets = null;
                    List<BinaryTextureImage> texture_headers = null;

                    // create material file path if it isn't set and it exists in
                    // the same directory as the input model file
                    /*if (mat_file == "") {
                        string inDir = Path.GetDirectoryName(in_file);
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(in_file);
                        string possible_matfile = Path.Combine(inDir, fileNameNoExt + "_mat.json");
                        if (File.Exists(possible_matfile)) {
                            mat_file = possible_matfile;
                        }
                    }*/

                    // Load material file
                    if (mat_file != "") {
                        JsonSerializer serializer = new JsonSerializer();

                        serializer.Converters.Add(
                            (new Newtonsoft.Json.Converters.StringEnumConverter())
                        );

                        using (TextReader file = File.OpenText(mat_file)) {
                            using (JsonTextReader reader = new JsonTextReader(file)) {
                                try {
                                    mat_presets = serializer.Deserialize<List<Material>>(reader);
                                }
                                catch (Newtonsoft.Json.JsonReaderException e) {
                                    Console.WriteLine(String.Format("Error encountered while reading {0}", mat_file));
                                    Console.WriteLine(String.Format("JsonReaderException: {0}", e.Message));
                                    return;
                                }
                                catch (Newtonsoft.Json.JsonSerializationException e) {
                                    Console.WriteLine(String.Format("Error encountered while reading {0}", mat_file));
                                    Console.WriteLine(String.Format("JsonSerializationException: {0}", e.Message));
                                    return;
                                }
                            }
                        }
                    }

                    // create texture header file path if it isn't set and it exists in the same
                    // directory as the input model file
                    /*if (texheader_file == "") {
                        string inDir = Path.GetDirectoryName(in_file);
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(in_file);
                        string possible_texfile = Path.Combine(inDir, fileNameNoExt + "_texheader.json");
                        if (File.Exists(possible_texfile)) {
                            texheader_file = possible_texfile;
                        }
                    }*/
                    string additionalTexPath = null; 
                    if (mat_file != "") {
                        additionalTexPath = Path.GetDirectoryName(mat_file);
                    }

                    Model mod = Model.Load(in_file, mat_presets, triopt, flipyz, fixNormals, additionalTexPath);

                    // Load texture headers
                    if (texheader_file != "") {
                        Console.WriteLine("Reading TexHeader");
                        JsonSerializer serializer = new JsonSerializer();

                        serializer.Converters.Add(
                            (new Newtonsoft.Json.Converters.StringEnumConverter())
                        );

                        using (TextReader file = File.OpenText(texheader_file)) {
                            using (JsonTextReader reader = new JsonTextReader(file)) {
                                texture_headers = serializer.Deserialize<List<BinaryTextureImage>>(reader);
                            }
                        }

                        foreach (BinaryTextureImage tex in mod.Textures.Textures) {
                            BinaryTextureImage found_tex = null;

                            foreach (BinaryTextureImage other in texture_headers) {
                                if (other != null) {
                                    if (other.Name == tex.Name) {
                                        found_tex = other;
                                        break;
                                    }
                                    else if (other.Name == "__TexDefault" && found_tex == null) {
                                        found_tex = other;
                                    }
                                }
                            }
                            Console.WriteLine("Hmm");
                            if (found_tex != null) {
                                Console.WriteLine(String.Format("Applying texture header preset for {0}: {1}", tex.Name, found_tex.Format));
                                tex.ReplaceHeaderInfo(found_tex);
                            }
                        }
                    }

                    if (out_file != "") {
                        mod.ExportBMD(out_file, true);
                    }
                    else {
                        mod.ExportBMD(in_file + ".bmd");
                    }
                }
                Console.WriteLine("Finished");
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
            Console.WriteLine("This is a fork maintained by Yoshi2 (RenolY2 on Github) with many additional features. Check the Readme.");
            Console.WriteLine("The project page of this fork is https://github.com/RenolY2/SuperBMD");
        }
    }
}
