using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperBMDLib.Rigging;
using OpenTK;
using GameFormatReader.Common;
using SuperBMDLib.Util;
using Assimp;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;

namespace SuperBMDLib.BMD
{
    public class JNT1
    {
        public List<Rigging.Bone> FlatSkeleton { get; private set; }
        public Dictionary<string, int> BoneNameIndices { get; private set; }
        public Rigging.Bone SkeletonRoot { get; private set; }

        public JNT1(EndianBinaryReader reader, int offset, BMDInfo modelstats=null)
        {
            BoneNameIndices = new Dictionary<string, int>();
            FlatSkeleton = new List<Rigging.Bone>();

            reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
            reader.SkipInt32();

            int jnt1Size = reader.ReadInt32();
            int jointCount = reader.ReadInt16();
            reader.SkipInt16();

            if (modelstats != null) {
                modelstats.JNT1Size = jnt1Size;
            }

            int jointDataOffset = reader.ReadInt32();
            int internTableOffset = reader.ReadInt32();
            int nameTableOffset = reader.ReadInt32();

            List<string> names = NameTableIO.Load(reader, offset + nameTableOffset);

            int highestRemap = 0;
            List<int> remapTable = new List<int>();
            reader.BaseStream.Seek(offset + internTableOffset, System.IO.SeekOrigin.Begin);
            for (int i = 0; i < jointCount; i++)
            {
                int test = reader.ReadInt16();
                remapTable.Add(test);

                if (test > highestRemap)
                    highestRemap = test;
            }

            List<Rigging.Bone> tempList = new List<Rigging.Bone>();
            reader.BaseStream.Seek(offset + jointDataOffset, System.IO.SeekOrigin.Begin);
            for (int i = 0; i <= highestRemap; i++)
            {
                tempList.Add(new Rigging.Bone(reader, names[i]));
            }

            for (int i = 0; i < jointCount; i++)
            {
                FlatSkeleton.Add(tempList[remapTable[i]]);
            }

            foreach (Rigging.Bone bone in FlatSkeleton)
                BoneNameIndices.Add(bone.Name, FlatSkeleton.IndexOf(bone));

            reader.BaseStream.Seek(offset + jnt1Size, System.IO.SeekOrigin.Begin);
        }

        public void SetInverseBindMatrices(List<Matrix4> matrices)
        {
            /*for (int i = 0; i < FlatSkeleton.Count; i++)
            {
                FlatSkeleton[i].SetInverseBindMatrix(matrices[i]);
            }*/
        }

        public static Assimp.Node GetRootBone(Assimp.Scene scene)
        {
            Assimp.Node root = null;
            List<string> bones = new List<string>();

            foreach (Assimp.Mesh mesh in scene.Meshes) {
                foreach (Assimp.Bone bone in mesh.Bones)
                {
                    bones.Add(bone.Name);
                }
            }

            if (bones.Count == 0)
            {
                Console.WriteLine("No bones found.");
                return root;
            }

            Stack<Assimp.Node> nodes_to_visit = new Stack<Assimp.Node>();
            nodes_to_visit.Push(scene.RootNode);

            while (nodes_to_visit.Count > 0)
            {
                Assimp.Node next = nodes_to_visit.Pop();

                // Check if it is a bone
                if (bones.Contains(next.Name)) 
                {
                    // Check if it is a root bone
                    if (next.Parent != null && !bones.Contains(next.Parent.Name))
                    {
                        Console.WriteLine("Found skeleton root: {0}", next.Name);
                        if (root != null)
                        {
                            throw new Exception(
                                String.Format("Cannot convert model: Found more than one root bone: {0}, {1}",
                                    root.Name, next.Name)
                                );
                        }
                        root = next;
                    }
                }
                foreach (Assimp.Node child in next.Children)
                {
                    nodes_to_visit.Push(child);
                }
            }

            return root;
        }

        public JNT1(Assimp.Scene scene, VTX1 vertexData)
        {
            BoneNameIndices = new Dictionary<string, int>();
            FlatSkeleton = new List<Rigging.Bone>();
            Assimp.Node root = GetRootBone(scene);
            
            /*for (int i = 0; i < scene.RootNode.ChildCount; i++)
            {
                if (scene.RootNode.Children[i].Name.ToLowerInvariant() == "skeleton_root")
                {
                    root = scene.RootNode.Children[i].Children[0];
                    break;
                }
                Console.Write(".");
            }*/

            if (root == null)
            {
                SkeletonRoot = new Rigging.Bone("root");
                SkeletonRoot.Bounds.GetBoundsValues(vertexData.Attributes.Positions);

                FlatSkeleton.Add(SkeletonRoot);
                BoneNameIndices.Add("root", 0);
            }

            else
            {
                SkeletonRoot = AssimpNodesToBonesRecursive(root, null, FlatSkeleton);
                
                

                foreach (Rigging.Bone bone in FlatSkeleton) {
                    //bone.m_MatrixType = 1;
                    //bone.m_UnknownIndex = 1;
                    BoneNameIndices.Add(bone.Name, FlatSkeleton.IndexOf(bone));
                }

                //FlatSkeleton[0].m_MatrixType = 0;
                //FlatSkeleton[0].m_UnknownIndex = 0;
            }
            Console.Write("✓");
            Console.WriteLine();
        }

        public void UpdateBoundingBoxes(VTX1 vertexData) {
            FlatSkeleton[0].Bounds.GetBoundsValues(vertexData.Attributes.Positions);
            for (int i = 1; i < FlatSkeleton.Count; i++) {
                FlatSkeleton[i].Bounds = FlatSkeleton[0].Bounds;
            }
        
        }

        private Rigging.Bone AssimpNodesToBonesRecursive(Assimp.Node node, Rigging.Bone parent, List<Rigging.Bone> boneList)
        {
            Rigging.Bone newBone = new Rigging.Bone(node, parent);
            boneList.Add(newBone);

            for (int i = 0; i < node.ChildCount; i++)
            {
                newBone.Children.Add(AssimpNodesToBonesRecursive(node.Children[i], newBone, boneList));
            }

            return newBone;
        }

        public void Write(EndianBinaryWriter writer)
        {
            long start = writer.BaseStream.Position;

            writer.Write("JNT1".ToCharArray());
            writer.Write(0); // Placeholder for section size
            writer.Write((short)FlatSkeleton.Count);
            writer.Write((short)-1);

            writer.Write(24); // Offset to joint data, always 24
            writer.Write(0); // Placeholder for remap data offset
            writer.Write(0); // Placeholder for name table offset

            List<string> names = new List<string>();
            foreach (Rigging.Bone bone in FlatSkeleton)
            {
                writer.Write(bone.ToBytes());
                names.Add(bone.Name);
            }

            long curOffset = writer.BaseStream.Position;

            writer.Seek((int)(start + 16), System.IO.SeekOrigin.Begin);
            writer.Write((int)(curOffset - start));
            writer.Seek((int)curOffset, System.IO.SeekOrigin.Begin);

            for (int i = 0; i < FlatSkeleton.Count; i++)
                writer.Write((short)i);

            StreamUtility.PadStreamWithString(writer, 4);

            curOffset = writer.BaseStream.Position;

            writer.Seek((int)(start + 20), System.IO.SeekOrigin.Begin);
            writer.Write((int)(curOffset - start));
            writer.Seek((int)curOffset, System.IO.SeekOrigin.Begin);

            NameTableIO.Write(writer, names);

            StreamUtility.PadStreamWithString(writer, 32);

            long end = writer.BaseStream.Position;
            long length = (end - start);

            writer.Seek((int)start + 4, System.IO.SeekOrigin.Begin);
            writer.Write((int)length);
            writer.Seek((int)end, System.IO.SeekOrigin.Begin);
        }

        public void DumpJson(string path) {
            JsonSerializer serial = new JsonSerializer();
            serial.Formatting = Formatting.Indented;
            serial.Converters.Add(new StringEnumConverter());


            using (FileStream strm = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                StreamWriter writer = new StreamWriter(strm);
                writer.AutoFlush = true;
                serial.Serialize(writer, this);
            }
        }
    }
}
