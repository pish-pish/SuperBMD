using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using Assimp;
using System.IO;
using SuperBMD.BMD;
using SuperBMD.Geometry.Enums;
using System.Collections;

namespace SuperBMD
{
    public class Model
    {
        public INF1 Scenegraph        { get; private set; }
        public VTX1 VertexData        { get; private set; }
        public EVP1 SkinningEnvelopes { get; private set; }
        public DRW1 PartialWeightData { get; private set; }
        public JNT1 Joints            { get; private set; }
        public SHP1 Shapes            { get; private set; }
        public MAT3 Materials         { get; private set; }
        public TEX1 Textures          { get; private set; }

        private int packetCount;
        private int vertexCount;

        public static Model Load(
            string filePath, List<Materials.Material> mat_presets = null, 
            TristripOption triopt = TristripOption.DoNotTriStrip,
            bool flipAxis = false)
        {
            string extension = Path.GetExtension(filePath);
            Model output = null;

            if (extension == ".bmd" || extension == ".bdl")
            {
                using (FileStream str = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    EndianBinaryReader reader = new EndianBinaryReader(str, Endian.Big);
                    output = new Model(reader, mat_presets);
                }
            }
            else
            {
                Assimp.AssimpContext cont = new Assimp.AssimpContext();

                // AssImp adds dummy nodes for pivots from FBX, so we'll force them off
                cont.SetConfig(new Assimp.Configs.FBXPreservePivotsConfig(false));

                Assimp.PostProcessSteps postprocess = Assimp.PostProcessSteps.Triangulate | Assimp.PostProcessSteps.JoinIdenticalVertices;
                
                if (triopt == TristripOption.DoNotTriStrip) {
                    // By not joining identical vertices, the Tri Strip algorithm we use cannot make tristrips, 
                    // effectively disabling tri stripping
                    postprocess = Assimp.PostProcessSteps.Triangulate; 
                }
                Assimp.Scene aiScene = cont.ImportFile(filePath, postprocess);

                output = new Model(aiScene, filePath, mat_presets, triopt, flipAxis);
            }

            return output;
        }

        public Model(EndianBinaryReader reader, List<Materials.Material> mat_presets = null)
        {
            int j3d2Magic = reader.ReadInt32();
            int modelMagic = reader.ReadInt32();

            if (j3d2Magic != 0x4A334432)
                throw new Exception("Model was not a BMD or BDL! (J3D2 magic not found)");
            if ((modelMagic != 0x62646C34) && (modelMagic != 0x626D6433))
                throw new Exception("Model was not a BMD or BDL! (Model type was not bmd3 or bdl4)");

            int modelSize = reader.ReadInt32();
            int sectionCount = reader.ReadInt32();

            // Skip the dummy section, SVR3
            reader.Skip(16);

            Scenegraph        = new INF1(reader, 32);
            VertexData        = new VTX1(reader, (int)reader.BaseStream.Position);
            SkinningEnvelopes = new EVP1(reader, (int)reader.BaseStream.Position);
            PartialWeightData = new DRW1(reader, (int)reader.BaseStream.Position);
            Joints            = new JNT1(reader, (int)reader.BaseStream.Position);
            //Joints.SetInverseBindMatrices(SkinningEnvelopes.InverseBindMatrices);
            SkinningEnvelopes.SetInverseBindMatrices(Joints.FlatSkeleton);
            Shapes            = SHP1.Create(reader, (int)reader.BaseStream.Position);
            Shapes.SetVertexWeights(SkinningEnvelopes, PartialWeightData);
            Materials         = new MAT3(reader, (int)reader.BaseStream.Position, mat_presets);
            SkipMDL3(reader);
            Textures          = new TEX1(reader, (int)reader.BaseStream.Position);

            // This is useful for dumping material data to json
            Materials.FillTextureNames(Textures); 

            foreach (Geometry.Shape shape in Shapes.Shapes)
                packetCount += shape.Packets.Count;

            vertexCount = VertexData.Attributes.Positions.Count;
        }

        private void SkipMDL3(EndianBinaryReader reader)
        {
            if (reader.PeekReadInt32() == 0x4D444C33)
            {
                int mdl3Size = reader.ReadInt32At(reader.BaseStream.Position + 4);
                reader.Skip(mdl3Size);
            }
        }

        public Model(
            Scene scene, string modelDirectory, 
            List<Materials.Material> mat_presets = null, TristripOption triopt = TristripOption.DoNotTriStrip,
            bool flipAxis = false)
        {
            if (flipAxis) {
                Console.WriteLine("Flipping things");
                int i = 0;
                Matrix4x4 rotate = Matrix4x4.FromRotationX((float)(-(1 / 2.0) * Math.PI));
                //rotate = Matrix4x4.FromRotationZ((float)(-(1 / 2.0) * Math.PI));
                Matrix4x4 rotateinv = rotate;
                

                Matrix3x3 rotateXminus90 = Matrix3x3.FromRotationX((float)(-(1 / 2.0) * Math.PI));
                Matrix3x3 rotateXplus90 = Matrix3x3.FromRotationX((float)((1 / 2.0) * Math.PI));
                Matrix3x3 rotateYminus90 = Matrix3x3.FromRotationY((float)(-(1 / 2.0) * Math.PI));
                Matrix3x3 rotateYplus90 = Matrix3x3.FromRotationY((float)((1 / 2.0) * Math.PI));
                Matrix3x3 rotateZminus90 = Matrix3x3.FromRotationZ((float)(-(1 / 2.0) * Math.PI));
                Matrix3x3 rotateZplus90 = Matrix3x3.FromRotationZ((float)((1 / 2.0) * Math.PI));

                //Matrix3x3 rotvec = rotateZplus90 * rotateXplus90;
                //Matrix3x3 rotvec = rotateYplus90*rotateYplus90 * rotateZplus90 * rotateZplus90* rotateXplus90* rotateXplus90;

                rotateinv.Inverse();
                //rotate = Matrix4x4.FromRotationY((float)(-(1 / 2.0) * Math.PI));
                //rotate = Matrix4x4.FromRotationZ((float)(-(1 / 2.0) * Math.PI));
                Matrix4x4 rotateC = Matrix4x4.FromRotationX((float)(-(1 / 2.0) * Math.PI));
                Matrix4x4 trans;
                Assimp.Node root = null;

                for (i = 0; i < scene.RootNode.ChildCount; i++) {
                    if (scene.RootNode.Children[i].Name.ToLowerInvariant() == "skeleton_root") {
                        root = scene.RootNode.Children[i].Children[0];
                        break;
                    }
                }

                if (root != null) {
                    foreach (Mesh mesh in scene.Meshes) {
                        Console.WriteLine(mesh.Name);
                        Console.WriteLine(String.Format("Does it have bones? {0}", mesh.HasBones));

                        int j = 0;
                        Matrix3x3[] weightedmats = new Matrix3x3[mesh.Normals.Count];



                        List<VertexWeight>[] weightgrid = new List<VertexWeight>[mesh.VertexCount];

                        foreach (Assimp.Bone bone in mesh.Bones) {
                            bone.OffsetMatrix = rotateinv*bone.OffsetMatrix;
                            Matrix3x3 invbind = bone.OffsetMatrix;
                            //bind.Inverse();
                            //List<int> vertices = new List<VertexWeight>();

                            foreach (Assimp.VertexWeight weight in bone.VertexWeights) {
                                //Vector3D norm = mesh.Normals[weight.VertexID];



                                /*Matrix3x3 weightedcurrentmat = new Matrix3x3(
                                        weight.Weight * invbind.A1, weight.Weight * invbind.A2, weight.Weight * invbind.A1,
                                        weight.Weight * invbind.B1, weight.Weight * invbind.B2, weight.Weight * invbind.B3,
                                        weight.Weight * invbind.C1, weight.Weight * invbind.C2, weight.Weight * invbind.C3);

                                if (weightedmats[weight.VertexID] == null) {
                                    weightedmats[weight.VertexID] = weightedcurrentmat;
                                }
                                else {
                                    Matrix3x3 existingmat = weightedmats[weight.VertexID];
                                    weightedmats[weight.VertexID] = new Matrix3x3(
                                        existingmat.A1 + invbind.A1, existingmat.A2 + invbind.A2, existingmat.A3 + invbind.A3,
                                        existingmat.B1 + invbind.B1, existingmat.B2 + invbind.B2, existingmat.B3 + invbind.B3,
                                        existingmat.C1 + invbind.C1, existingmat.C2 + invbind.C2, existingmat.C3 + invbind.C3);
                                }*/
                            }

                            //Matrix4x4 bindMat = bone.OffsetMatrix;
                            //bindMat.Inverse();
                            //trans = 
                            /*bone.OffsetMatrix = root.Transform * bone.OffsetMatrix;
                            Matrix4x4 newtransform = root.Transform * rotate;
                            newtransform.Inverse();
                            bone.OffsetMatrix = newtransform * bone.OffsetMatrix;*/

                        }

                        for (i = 0; i < mesh.VertexCount; i++) {
                            Vector3D vertex = mesh.Vertices[i];
                            vertex.Set(vertex.X, vertex.Z, -vertex.Y);
                            mesh.Vertices[i] = vertex;
                        }
                        for (i = 0; i < mesh.Normals.Count; i++) {
                            Vector3D norm = mesh.Normals[i];
                            //norm.Set(norm.X, norm.Z, -norm.Y);
                            //Vector3D newnorm = rotvec*norm;
                            //Matrix3x3 mat = weightedmats[i];
                            //Vector3D newnorm = mat * norm; 
                            norm.Set(-norm.Y, -norm.Z, norm.X); //<-- pretty good tbh
                            //norm.Set(-norm.Y, -norm.Z, -norm.X); //<- not bad either??
                            //norm.Set(norm.X, -norm.Z, -norm.Y); //<- ok

                            //norm.Set((float)1.0, (float)0.0, (float)0.0);
                            mesh.Normals[i] = norm;
                        }
                    }
                }

                

                if (root != null) {
                    
                    List<Assimp.Node> allnodes = new List<Assimp.Node>();
                    List<Assimp.Node> processnodes = new List<Assimp.Node>();
                    processnodes.Add(root);
                    root.Transform = root.Transform * rotate;
                    /*while (processnodes.Count > 0) {
                        Assimp.Node current = processnodes[0];
                        processnodes.RemoveAt(0);

                        current.Transform = current.Transform * rotate;

                        foreach (Assimp.Node child in current.Children) {
                            processnodes.Add(child);
                        }
                    }*/

                }

            }

            flipAxis = false;
            VertexData = new VTX1(scene, flipAxis);
            Joints = new JNT1(scene, VertexData, flipAxis);
            Scenegraph = new INF1(scene, Joints);
            Textures = new TEX1(scene, Path.GetDirectoryName(modelDirectory));

            SkinningEnvelopes = new EVP1();
            SkinningEnvelopes.SetInverseBindMatrices(scene, Joints.FlatSkeleton);
            //SkinningEnvelopes.AddInverseBindMatrices(Joints.FlatSkeleton);

            PartialWeightData = new DRW1(scene, Joints.BoneNameIndices);

            Shapes = SHP1.Create(scene, Joints.BoneNameIndices, VertexData.Attributes, SkinningEnvelopes, PartialWeightData, triopt);
            if (flipAxis) {
                Shapes.flipAxis();
            }
            Materials = new MAT3(scene, Textures, Shapes, mat_presets);
            Materials.LoadAdditionalTextures(Textures, modelDirectory);
            Materials.MapTextureNamesToIndices(Textures);

            foreach (Geometry.Shape shape in Shapes.Shapes)
                packetCount += shape.Packets.Count;

            vertexCount = VertexData.Attributes.Positions.Count;
        }

        public void ExportBMD(string fileName, bool overwrite = false)
        {
            string outDir = Path.GetDirectoryName(fileName);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
            fileNameNoExt = fileNameNoExt.Split('.')[0];
            fileName = Path.Combine(outDir, fileNameNoExt + ".bmd");

            if (File.Exists(fileName) && overwrite == false)
            {
                fileName = Path.Combine(outDir, fileNameNoExt + "_2.bmd");
            }

            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                EndianBinaryWriter writer = new EndianBinaryWriter(stream, Endian.Big);

                writer.Write("J3D2bmd3".ToCharArray());
                writer.Write(0); // Placeholder for file size
                writer.Write(8); // Number of sections; bmd has 8, bdl has 9

                writer.Write("SuperBMD - Gamma".ToCharArray());

                Scenegraph.Write(writer, packetCount, vertexCount);
                VertexData.Write(writer);
                SkinningEnvelopes.Write(writer);
                PartialWeightData.Write(writer);
                Joints.Write(writer);
                Shapes.Write(writer);
                Materials.Write(writer);
                Textures.Write(writer);

                writer.Seek(8, SeekOrigin.Begin);
                writer.Write((int)writer.BaseStream.Length);
            }
        }

        public void ExportAssImp(string fileName, string outFilepath, string modelType, ExportSettings settings, bool keepmatnames = false)
        {
            string outDir = Path.GetDirectoryName(outFilepath);
            //string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
            fileName = outFilepath;//Path.Combine(outDir, fileNameNoExt + ".dae");

            Scene outScene = new Scene();

            outScene.RootNode = new Node("RootNode");

            Scenegraph.FillScene(outScene, Joints.FlatSkeleton, settings.UseSkeletonRoot);
            Materials.FillScene(outScene, Textures, outDir);
            Shapes.FillScene(outScene, VertexData.Attributes, Joints.FlatSkeleton, SkinningEnvelopes.InverseBindMatrices);
            Scenegraph.CorrectMaterialIndices(outScene, Materials);
            Textures.DumpTextures(outDir);

            if (SkinningEnvelopes.Weights.Count == 0)
            {
                Assimp.Node geomNode = new Node(Path.GetFileNameWithoutExtension(fileName), outScene.RootNode);

                for (int i = 0; i < Shapes.Shapes.Count; i++)
                {
                    geomNode.MeshIndices.Add(i);
                }

                outScene.RootNode.Children.Add(geomNode);
            }

            AssimpContext cont = new AssimpContext();

            cont.ExportFile(outScene, fileName, "collada", PostProcessSteps.ValidateDataStructure | PostProcessSteps.JoinIdenticalVertices);
            //cont.ExportFile(outScene, fileName, "collada");


            if (SkinningEnvelopes.Weights.Count == 0)
                return; // There's no skinning information, so we can stop here

            // Now we need to add some skinning info, since AssImp doesn't do it for some bizarre reason

            StreamWriter test = new StreamWriter(fileName + ".tmp");
            StreamReader dae = File.OpenText(fileName);
            
            while (!dae.EndOfStream)
            {
                string line = dae.ReadLine();

                if (line == "  <library_visual_scenes>")
                {
                    AddControllerLibrary(outScene, test);
                    test.WriteLine(line);
                    test.Flush();
                }
                else if (line.Contains("<node"))
                {
                    string[] testLn = line.Split('\"');
                    string name = testLn[3];

                    if (Joints.FlatSkeleton.Exists(x => x.Name == name))
                    {
                        string jointLine = line.Replace(">", $" sid=\"{ name }\" type=\"JOINT\">");
                        test.WriteLine(jointLine);
                        test.Flush();
                    }
                    else
                    {
                        test.WriteLine(line);
                        test.Flush();
                    }
                }
                else if (line.Contains("</visual_scene>"))
                {
                    foreach (Mesh mesh in outScene.Meshes)
                    {
                        string matname = "mat";
                        if (keepmatnames == true) {
                            // attempt to keep the original material names
                            // this is experimental because AssImp sanitizes the names
                            matname = outScene.Materials[mesh.MaterialIndex].Name.Replace("#", "_").Replace(" ", "_");
                        }

                        test.WriteLine($"      <node id=\"{ mesh.Name }\" name=\"{ mesh.Name }\" type=\"NODE\">");

                        test.WriteLine($"       <instance_controller url=\"#{ mesh.Name }-skin\">");
                        test.WriteLine("        <skeleton>#skeleton_root</skeleton>");
                        test.WriteLine("        <bind_material>");
                        test.WriteLine("         <technique_common>");
                        test.WriteLine($"          <instance_material symbol=\"theresonlyone\" target=\"#m{ mesh.MaterialIndex }{matname}\" />");
                        test.WriteLine("         </technique_common>");
                        test.WriteLine("        </bind_material>");
                        test.WriteLine("       </instance_controller>");

                        test.WriteLine("      </node>");
                        test.Flush();
                    }

                    test.WriteLine(line);
                    test.Flush();
                }
                else if (line.Contains("<matrix"))
                {
                    string matLine = line.Replace("<matrix>", "<matrix sid=\"matrix\">");
                    test.WriteLine(matLine);
                    test.Flush();
                }
                else
                {
                    test.WriteLine(line);
                    test.Flush();
                }
            }

            test.Close();
            dae.Close();

            File.Copy(fileName + ".tmp", fileName, true);
            File.Delete(fileName + ".tmp");
        }

        private void AddControllerLibrary(Scene scene, StreamWriter writer)
        {
            writer.WriteLine("  <library_controllers>");

            for (int i = 0; i < scene.MeshCount; i++)
            {
                Mesh curMesh = scene.Meshes[i];
                curMesh.Name = curMesh.Name.Replace('_', '-');

                writer.WriteLine($"   <controller id=\"{ curMesh.Name }-skin\" name=\"{ curMesh.Name }Skin\">");

                writer.WriteLine($"    <skin source=\"#meshId{ i }\">");

                WriteBindShapeMatrixToStream(writer);
                WriteJointNameArrayToStream(curMesh, writer);
                WriteInverseBindMatricesToStream(curMesh, writer);
                WriteSkinWeightsToStream(curMesh, writer);

                writer.WriteLine("     <joints>");

                writer.WriteLine($"      <input semantic=\"JOINT\" source=\"#{ curMesh.Name }-skin-joints-array\"></input>");
                writer.WriteLine($"      <input semantic=\"INV_BIND_MATRIX\" source=\"#{ curMesh.Name }-skin-bind_poses-array\"></input>");

                writer.WriteLine("     </joints>");
                writer.Flush();

                WriteVertexWeightsToStream(curMesh, writer);

                writer.WriteLine("    </skin>");

                writer.WriteLine("   </controller>");
                writer.Flush();
            }

            writer.WriteLine("  </library_controllers>");
            writer.Flush();
        }

        private void WriteBindShapeMatrixToStream(StreamWriter writer)
        {
            writer.WriteLine("     <bind_shape_matrix>");

            writer.WriteLine("      1 0 0 0");
            writer.WriteLine("      0 1 0 0");
            writer.WriteLine("      0 0 1 0");
            writer.WriteLine("      0 0 0 1");

            writer.WriteLine("     </bind_shape_matrix>");
            writer.Flush();
        }

        private void WriteJointNameArrayToStream(Mesh mesh, StreamWriter writer)
        {
            writer.WriteLine($"      <source id =\"{ mesh.Name }-skin-joints-array\">");
            writer.WriteLine($"      <Name_array id=\"{ mesh.Name }-skin-joints-array\" count=\"{ mesh.Bones.Count }\">");

            writer.Write("       ");
            foreach (Bone bone in mesh.Bones)
            {
                writer.Write($"{ bone.Name }");
                if (bone != mesh.Bones.Last())
                    writer.Write(' ');
                else
                    writer.Write('\n');

                writer.Flush();
            }

            writer.WriteLine("      </Name_array>");
            writer.Flush();

            writer.WriteLine("      <technique_common>");
            writer.WriteLine($"       <accessor source=\"#{ mesh.Name }-skin-joints-array\" count=\"{ mesh.Bones.Count }\" stride=\"1\">");
            writer.WriteLine("         <param name=\"JOINT\" type=\"Name\"></param>");
            writer.WriteLine("       </accessor>");
            writer.WriteLine("      </technique_common>");
            writer.WriteLine("      </source>");
            writer.Flush();
        }

        private void WriteInverseBindMatricesToStream(Mesh mesh, StreamWriter writer)
        {
            writer.WriteLine($"      <source id =\"{ mesh.Name }-skin-bind_poses-array\">");
            writer.WriteLine($"      <float_array id=\"{ mesh.Name }-skin-bind_poses-array\" count=\"{ mesh.Bones.Count * 16 }\">");

            foreach (Bone bone in mesh.Bones)
            {
                Matrix4x4 ibm = bone.OffsetMatrix;
                //ibm.Transpose();

                writer.WriteLine($"       {ibm.A1.ToString("F")} {ibm.A2.ToString("F")} {ibm.A3.ToString("F")} {ibm.A4.ToString("F")}");
                writer.WriteLine($"       {ibm.B1.ToString("F")} {ibm.B2.ToString("F")} {ibm.B3.ToString("F")} {ibm.B4.ToString("F")}");
                writer.WriteLine($"       {ibm.C1.ToString("F")} {ibm.C2.ToString("F")} {ibm.C3.ToString("F")} {ibm.C4.ToString("F")}");
                writer.WriteLine($"       {ibm.D1.ToString("F")} {ibm.D2.ToString("F")} {ibm.D3.ToString("F")} {ibm.D4.ToString("F")}");

                if (bone != mesh.Bones.Last())
                    writer.WriteLine("");
            }

            writer.WriteLine("      </float_array>");
            writer.Flush();

            writer.WriteLine("      <technique_common>");
            writer.WriteLine($"       <accessor source=\"#{ mesh.Name }-skin-bind_poses-array\" count=\"{ mesh.Bones.Count }\" stride=\"16\">");
            writer.WriteLine("         <param name=\"TRANSFORM\" type=\"float4x4\"></param>");
            writer.WriteLine("       </accessor>");
            writer.WriteLine("      </technique_common>");
            writer.WriteLine("      </source>");
            writer.Flush();
        }

        private void WriteSkinWeightsToStream(Mesh mesh, StreamWriter writer)
        {
            int totalWeightCount = 0;

            foreach (Bone bone in mesh.Bones)
            {
                totalWeightCount += bone.VertexWeightCount;
            }

            writer.WriteLine($"      <source id =\"{ mesh.Name }-skin-weights-array\">");
            writer.WriteLine($"      <float_array id=\"{ mesh.Name }-skin-weights-array\" count=\"{ totalWeightCount }\">");
            writer.Write("       ");

            foreach (Bone bone in mesh.Bones)
            {
                foreach (VertexWeight weight in bone.VertexWeights)
                {
                    writer.Write($"{ weight.Weight } " );
                }

                if (bone == mesh.Bones.Last())
                    writer.WriteLine();
            }

            writer.WriteLine("      </float_array>");
            writer.Flush();

            writer.WriteLine("      <technique_common>");
            writer.WriteLine($"       <accessor source=\"#{ mesh.Name }-skin-weights-array\" count=\"{ totalWeightCount }\" stride=\"1\">");
            writer.WriteLine("         <param name=\"WEIGHT\" type=\"float\"></param>");
            writer.WriteLine("       </accessor>");
            writer.WriteLine("      </technique_common>");
            writer.WriteLine("      </source>");
            writer.Flush();
        }

        private void WriteVertexWeightsToStream(Mesh mesh, StreamWriter writer)
        {
            List<float> weights = new List<float>();
            Dictionary<int, Rigging.Weight> vertIDWeights = new Dictionary<int, Rigging.Weight>();

            foreach (Bone bone in mesh.Bones)
            {
                foreach (VertexWeight weight in bone.VertexWeights)
                {
                    weights.Add(weight.Weight);

                    if (!vertIDWeights.ContainsKey(weight.VertexID))
                        vertIDWeights.Add(weight.VertexID, new Rigging.Weight());

                    vertIDWeights[weight.VertexID].AddWeight(weight.Weight, mesh.Bones.IndexOf(bone));
                }
            }

            writer.WriteLine($"      <vertex_weights count=\"{ vertIDWeights.Count }\">");

            writer.WriteLine($"       <input semantic=\"JOINT\" source=\"#{ mesh.Name }-skin-joints-array\" offset=\"0\"></input>");
            writer.WriteLine($"       <input semantic=\"WEIGHT\" source=\"#{ mesh.Name }-skin-weights-array\" offset=\"1\"></input>");

            writer.WriteLine("       <vcount>");

            writer.Write("        ");
            for (int i = 0; i < vertIDWeights.Count; i++)
                writer.Write($"{ vertIDWeights[i].WeightCount } ");

            writer.WriteLine("\n       </vcount>");

            writer.WriteLine("       <v>");
            writer.Write("        ");

            for (int i = 0; i < vertIDWeights.Count; i++)
            {
                Rigging.Weight curWeight = vertIDWeights[i];

                for (int j = 0; j < curWeight.WeightCount; j++)
                {
                    writer.Write($"{ curWeight.BoneIndices[j] } { weights.IndexOf(curWeight.Weights[j]) } ");
                }
            }

            writer.WriteLine("\n       </v>");

            writer.WriteLine($"      </vertex_weights>");
        }
    }
}
