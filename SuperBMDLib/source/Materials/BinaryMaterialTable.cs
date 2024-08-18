using GameFormatReader.Common;
using SuperBMDLib;
using SuperBMDLib.Util;
using SuperBMDLib.BMD;
using SuperBMDLib.Materials;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SuperBMD.source.Materials
{
    public class BinaryMaterialTable : J3DHeader
    {
        public MAT3 Materials { get; private set; }
        public TEX1 Textures { get; private set; }

        protected override string FileMagic => "J3D2bmt3";

        public BinaryMaterialTable(MAT3 materials, TEX1 textures)
        {
            Materials = materials;
            Textures = textures;
        }

        public BinaryMaterialTable(EndianBinaryReader reader) : base(reader)
        {
            SectionCount = 1;

            Materials = new MAT3(reader, (int)reader.BaseStream.Position);
            Textures = null;

            byte[] magicByes = reader.PeekReadBytes(4);
            if (Encoding.UTF8.GetString(magicByes) == "TEX1")
            {
                SectionCount = 2;
                Textures = new TEX1(reader, (int)reader.BaseStream.Position);
                Materials.SetTextureNames(Textures);
            }
        }

        public BinaryMaterialTable(Arguments args, List<Material> mat_presets, string additionalTexPath)
        {
            if (mat_presets == null)
            {
                throw new ArgumentException("Material JSON files are required to create a BMT!");
            }

            SectionCount = 1;

            Console.WriteLine("Generating the Material Data ->");
            Materials = new MAT3(mat_presets);

            Textures = null;
            if (args.texheaders_path == "")
            {
                return;
            }

            SectionCount = 2;

            Console.WriteLine("Generating the Texture Data -> ");
            Textures = new TEX1(args);

            Console.WriteLine("Loading the Textures ->");
            if (additionalTexPath == null)
            {
                Materials.LoadAdditionalTextures(Textures, Path.GetDirectoryName(args.input_path), args.readMipmaps);
            }
            else
            {
                Materials.LoadAdditionalTextures(Textures, additionalTexPath, args.readMipmaps);
            }

            Materials.MapTextureNamesToIndices(Textures);
        }

        public static void DumpContents(Arguments args)
        {
            BinaryMaterialTable table;
            using (FileStream stream = new FileStream(args.input_path, FileMode.Open, FileAccess.Read))
            {
                EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);
                table = new BinaryMaterialTable(reader);
            }

            string inDir = Path.GetDirectoryName(args.input_path);
            string filenameNoExt = Path.GetFileNameWithoutExtension(args.input_path);

            if (args.output_materials_path != "")
            {
                table.Materials.DumpMaterials(Path.GetDirectoryName(args.output_materials_path));
            }
            else
            {
                table.Materials.DumpMaterials(Path.Combine(inDir, filenameNoExt + "_materials.json"));
            }

            table.Textures?.DumpTextures(inDir, filenameNoExt + "_tex_headers.json", true, args.readMipmaps);

            if (args.output_material_folder != "")
            {
                table.Materials.DumpMaterialsFolder(args.output_material_folder);
            }
        }

        public void ExportBMT(string fileName)
        {
            fileName = Path.GetFullPath(fileName); // Get absolute path instead of relative
            string outDir = Path.GetDirectoryName(fileName);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);

            Console.WriteLine($"Creating new BMT with name \"{fileNameNoExt}\"...");

            fileName = Path.Combine(outDir, fileNameNoExt + ".bmt");
            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                EndianBinaryWriter writer = new EndianBinaryWriter(stream, Endian.Big);
                Write(writer);
                Materials.Write(writer);
                Textures?.Write(writer);
                WriteSize(writer);
            }
        }
    }
}
