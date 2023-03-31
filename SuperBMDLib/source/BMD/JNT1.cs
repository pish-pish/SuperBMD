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

        public static bool IsChildOf(Assimp.Node root, Assimp.Node child)
        {
            Stack<Assimp.Node> nodes_to_visit = new Stack<Assimp.Node>();
            if (root == child)
            {
                return true;
            }

            nodes_to_visit.Push(root);

            while (nodes_to_visit.Count > 0)
            {
                Assimp.Node next = nodes_to_visit.Pop();
                if (next == child) {
                    return true;
                }

                foreach (Assimp.Node nextchild in next.Children)
                {
                    nodes_to_visit.Push(nextchild);
                }
            }
            return false;
        }

        public static bool IsChildOf(Assimp.Node root, string bonename)
        {
            Stack<Assimp.Node> nodes_to_visit = new Stack<Assimp.Node>();
            if (root.Name == bonename)
            {
                return true;
            }

            nodes_to_visit.Push(root);

            while (nodes_to_visit.Count > 0)
            {
                Assimp.Node next = nodes_to_visit.Pop();
                if (next.Name == bonename)
                {
                    return true;
                }

                foreach (Assimp.Node nextchild in next.Children)
                {
                    nodes_to_visit.Push(nextchild);
                }
            }
            return false;
        }

        public static List<Assimp.Node> getMeshNodes(Assimp.Scene scene)
        {
            Stack<Assimp.Node> nodes_to_visit = new Stack<Assimp.Node>();
            nodes_to_visit.Push(scene.RootNode);
            List<Assimp.Node> meshnodes = new List<Assimp.Node>();

            while (nodes_to_visit.Count > 0)
            {
                Assimp.Node next = nodes_to_visit.Pop();
                if (next.HasMeshes)
                {
                    meshnodes.Add(next);
                }

                foreach (Assimp.Node nextchild in next.Children)
                {
                    nodes_to_visit.Push(nextchild);
                }
            }

            return meshnodes;
        }


        public static Assimp.Node GetRootBone(Assimp.Scene scene, string root_marker, string root_name, bool auto_detect)
        {
            Assimp.Node root = null;
            
            if (auto_detect) { 
                Console.WriteLine("Attempting automated skeleton root detection...");
                Assimp.Bone bone = null;
                Assimp.Node meshparent = null;
                foreach (Assimp.Mesh mesh in scene.Meshes)
                {
                    if (bone != null) { 
                        foreach (Assimp.Bone meshbone in mesh.Bones)
                        {
                            bone = meshbone;
                            break;
                        }
                    }
                }



                Assimp.Node parent = null;
                foreach (Assimp.Node meshnode in getMeshNodes(scene))
                {
                    if (parent == null) { 
                        parent = meshnode.Parent;
                    }
                    else if (parent != meshnode.Parent)
                    {
                        if (!IsChildOf(parent, meshnode))
                        {
                            Console.WriteLine("Warning, node {0} is not child of detected skeleton root {1}", meshnode.Name, parent.Name);
                        }
                    }
                } 
                if (bone != null)
                {
                    foreach (Assimp.Node child in parent.Children)
                    {
                        if (!child.HasMeshes)
                        {
                            if (IsChildOf(child, bone.Name))
                            {
                                Console.WriteLine("Detected {0} as root bone.", child.Name);
                                root = child;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Assimp.Node candidate = null;
                    bool more_than_one = false;
                    foreach (Assimp.Node child in parent.Children)
                    {
                        if (!child.HasMeshes)
                        {
                            if (candidate == null) { 
                                candidate = child;
                            }
                            else
                            {
                                more_than_one = false;
                                Console.WriteLine("Cannot auto-detect, there are at least two skeleton root candidates {0}, {1}", candidate.Name, child.Name);
                                break;
                            }
                        }
                    }

                    if (!more_than_one) {
                        root = candidate;
                    }
                }
                if (root != null)
                {
                    Console.WriteLine("Detected {0} as root bone.", root.Name);
                    return root;
                }
                else
                {
                    Console.WriteLine("Auto-detection failed. Falling back onto searching for skeleton root object.");
                }
            }

            // Search for skeleton root marker
            for (int i = 0; i < scene.RootNode.ChildCount; i++) {
                Console.WriteLine(scene.RootNode.Children[i].Name);
                if (scene.RootNode.Children[i].Name == root_marker) {
                    if (scene.RootNode.Children[i].ChildCount == 0)
                    {
                        throw new System.Exception(
                            String.Format(
                                "{0} has no children! If you are making a rigged model, make sure {0} contains the root of your skeleton.",
                                root_marker)
                            );
                    }
                    Assimp.NodeCollection skeleton_root_children = scene.RootNode.Children[i].Children;
                    root = skeleton_root_children[0];
                    if (root.HasMeshes)
                    {
                        Console.WriteLine("Detected root bone {0} has meshes so probably isn't a root bone. Continuing search.", root.Name);
                        bool success = false;
                        for (int j = 1; j < skeleton_root_children.Count; j++)
                        {
                            if (!skeleton_root_children[j].HasMeshes)
                            {
                                root = skeleton_root_children[j];
                                Console.WriteLine("Chosen {0} as root bone.", root.Name);
                                success = true;
                                break;
                            }
                        }
                        if (!success)
                        {
                            throw new Exception("Failed to detect non-mesh object that could be a root bone, cannot continue.");
                        }

                    }
                    break;
                }
                Console.Write(".");
            }

            if (root_name == null)
            {
                return root;
            }

            // Search for skeleton root bone
            Queue<Assimp.Node> nodes_to_visit = new Queue<Assimp.Node>();
            nodes_to_visit.Enqueue(scene.RootNode);

            Console.WriteLine("traversing hierarchy to search for {0}", root_name);
            while (nodes_to_visit.Count > 0)
            {
                Assimp.Node next = nodes_to_visit.Dequeue();
                if (next.Name == root_name)
                {
                    if (root == null) { 
                        root = next;
                        break;
                    }
                }
                foreach (Assimp.Node child in next.Children)
                {
                    nodes_to_visit.Enqueue(child);
                }
            }
            if (root != null) { 
                Console.WriteLine("Choosen root bone: {0}", root.Name);
            }
            else
            {
                Console.WriteLine("No root chosen.");
            }
            return root;
        }

        public JNT1(Assimp.Scene scene, VTX1 vertexData, Arguments args)
        {
            BoneNameIndices = new Dictionary<string, int>();
            FlatSkeleton = new List<Rigging.Bone>();
            Assimp.Node root = GetRootBone(scene, args.skeleton_root_marker, args.skeleton_root_name, args.skeleton_autodetect);
            
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
