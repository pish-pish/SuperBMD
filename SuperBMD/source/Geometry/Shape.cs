using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperBMD.Util;
using GameFormatReader.Common;
using OpenTK;
using Assimp;
using SuperBMD.BMD;
using SuperBMD.Rigging;
using BrawlLib.Modeling.Triangle_Converter;

namespace SuperBMD.Geometry
{
    public class Shape
    {
        public VertexData AttributeData { get; private set; }
        public ShapeVertexDescriptor Descriptor { get; private set; }

        public byte MatrixType { get; private set; }
        public BoundingVolume Bounds { get; private set; }

        public List<Packet> Packets { get; private set; }

        private Vector4[] m_PositionMatrices;
        private Vector4[] m_NormalMatrices;

        public Shape()
        {
            MatrixType = 3;
            AttributeData = new VertexData();
            Descriptor = new ShapeVertexDescriptor();
            Packets = new List<Packet>();
            Bounds = new BoundingVolume();

            m_PositionMatrices = new Vector4[64];
            m_NormalMatrices = new Vector4[32];
        }

        public Shape(ShapeVertexDescriptor desc, BoundingVolume bounds, List<Packet> prims, int matrixType)
        {
            Descriptor = desc;
            Bounds = bounds;
            Packets = prims;
            MatrixType = (byte)matrixType;
        }

        public void SetDescriptorAttributes(Mesh mesh, int jointCount)
        {
            int indexOffset = 0;

            if (jointCount > 1)
                Descriptor.SetAttribute(Enums.GXVertexAttribute.PositionMatrixIdx, Enums.VertexInputType.Direct, indexOffset++);

            if (mesh.HasVertices)
                Descriptor.SetAttribute(Enums.GXVertexAttribute.Position, Enums.VertexInputType.Index16, indexOffset++);
            if (mesh.HasNormals)
                Descriptor.SetAttribute(Enums.GXVertexAttribute.Normal, Enums.VertexInputType.Index16, indexOffset++);
            for (int i = 0; i < 2; i++)
            {
                if (mesh.HasVertexColors(i))
                    Descriptor.SetAttribute(Enums.GXVertexAttribute.Color0 + i, Enums.VertexInputType.Index16, indexOffset++);
            }

            for (int i = 0; i < 8; i++)
            {
                if (mesh.HasTextureCoords(i))
                    Descriptor.SetAttribute(Enums.GXVertexAttribute.Tex0 + i, Enums.VertexInputType.Index16, indexOffset++);
            }
        }

        uint[] WeldVertices(Mesh mesh) {
            uint[] triindices = new uint[mesh.Faces.Count * 3];

            int i = 0;
            foreach (Face face in mesh.Faces) {
                //bool found = false;
                //int vertexindex;
                for (int j = 0; j < 3; j++) {
                    //vertexindex = (face.Indices[j]);
                    //found = false;
                    triindices[i * 3 + j] = (uint)face.Indices[j];
                    /*for (int k = 0; k < mesh.Vertices.Count; k++) {
                        if (mesh.Vertices[k].Equals(mesh.Vertices[vertexindex])
                            && (mesh.Normals.Count == 0 || mesh.Normals[k].Equals(mesh.Normals[vertexindex]))) {
                            bool texchannels_same = true;

                            for (int l = 0; l < mesh.TextureCoordinateChannelCount; l++) {
                                Vector3D curr = mesh.TextureCoordinateChannels[l][vertexindex];
                                Vector3D other = mesh.TextureCoordinateChannels[l][k];
                                if (other.Equals(curr) == false) {
                                    texchannels_same = false;
                                }
                            }
                            bool vertexchannels_same = true;
                            for (int l = 0; l < mesh.VertexColorChannelCount; l++) {
                                Color4D curr = mesh.VertexColorChannels[l][vertexindex];
                                Color4D other = mesh.VertexColorChannels[l][k];
                                if (other.Equals(curr) == false) {
                                    vertexchannels_same = false;
                                }
                            }

                            if ((texchannels_same == true) && (vertexchannels_same == true)) {
                                triindices[i * 3 + j] = (uint)k;
                                found = true;
                                break;
                            }
                        }
                    }*/

                    //if (found == false) {
                    //    triindices[i * 3 + j] = (uint)vertexindex;
                    //}
                }

                i += 1;
            }
            return triindices;
        }

        public void ProcessVerticesWithoutWeights(Mesh mesh, VertexData vertData)
        {
            Packet pack = new Packet();

            
            List<Enums.GXVertexAttribute> activeAttribs = Descriptor.GetActiveAttributes();
            AttributeData.SetAttributesFromList(activeAttribs);

            Console.WriteLine("Calculating triangle strips");

            uint[] triindices = WeldVertices(mesh);
            TriStripper stripper = new TriStripper(triindices);
            List<PrimitiveBrawl> primlist = stripper.Strip();

            Console.WriteLine(String.Format("Done, {0} primitives", primlist.Count));

            //for (int i = 0; i < mesh.FaceCount*3; i++) {
            //    triindices[i] = 0;
            //}

            // foreach (Face face in mesh.Faces)
            foreach (PrimitiveBrawl primbrawl in primlist)
            {
                //Primitive prim = new Primitive(Enums.GXPrimitiveType.TriangleStrip);
                Primitive prim = new Primitive((Enums.GXPrimitiveType)primbrawl.Type);
                //Console.WriteLine(String.Format("Primitive type {0}", (Enums.GXPrimitiveType)primbrawl.Type));
                foreach (int vertIndex in primbrawl.Indices)
                {
                    Vertex vert = new Vertex();

                    Weight rootWeight = new Weight();
                    rootWeight.AddWeight(1.0f, 0);

                    vert.SetWeight(rootWeight);
                    //int vertIndex = face.Indices[i];

                    foreach (Enums.GXVertexAttribute attrib in activeAttribs) {
                        switch (attrib) {
                            case Enums.GXVertexAttribute.Position:
                                List<Vector3> posData = (List<Vector3>)vertData.GetAttributeData(Enums.GXVertexAttribute.Position);
                                Vector3 vertPos = mesh.Vertices[vertIndex].ToOpenTKVector3();

                                
                                AttributeData.Positions.Add(vertPos);


                                vert.SetAttributeIndex(Enums.GXVertexAttribute.Position, (uint)posData.IndexOf(vertPos));
                                break;
                            case Enums.GXVertexAttribute.Normal:
                                List<Vector3> normData = (List<Vector3>)vertData.GetAttributeData(Enums.GXVertexAttribute.Normal);
                                Vector3 vertNrm = mesh.Normals[vertIndex].ToOpenTKVector3();

                                
                                AttributeData.Normals.Add(vertNrm);

                                vert.SetAttributeIndex(Enums.GXVertexAttribute.Normal, (uint)normData.IndexOf(vertNrm));
                                break;
                            case Enums.GXVertexAttribute.Color0:
                            case Enums.GXVertexAttribute.Color1:
                                int colNo = (int)attrib - 11;
                                List<Color> colData = (List<Color>)vertData.GetAttributeData(Enums.GXVertexAttribute.Color0 + colNo);
                                Color vertCol = mesh.VertexColorChannels[colNo][vertIndex].ToSuperBMDColorRGBA();

                                
                                if (colNo == 0)
                                    AttributeData.Color_0.Add(vertCol);
                                else
                                    AttributeData.Color_1.Add(vertCol);
                                

                                vert.SetAttributeIndex(Enums.GXVertexAttribute.Color0 + colNo, (uint)colData.IndexOf(vertCol));
                                break;
                            case Enums.GXVertexAttribute.Tex0:
                            case Enums.GXVertexAttribute.Tex1:
                            case Enums.GXVertexAttribute.Tex2:
                            case Enums.GXVertexAttribute.Tex3:
                            case Enums.GXVertexAttribute.Tex4:
                            case Enums.GXVertexAttribute.Tex5:
                            case Enums.GXVertexAttribute.Tex6:
                            case Enums.GXVertexAttribute.Tex7:
                                int texNo = (int)attrib - 13;
                                List<Vector2> texCoordData = (List<Vector2>)vertData.GetAttributeData(Enums.GXVertexAttribute.Tex0 + texNo);
                                Vector2 vertTexCoord = mesh.TextureCoordinateChannels[texNo][vertIndex].ToOpenTKVector2();
                                vertTexCoord = new Vector2(vertTexCoord.X, 1.0f - vertTexCoord.Y);

 
                                switch (texNo) {
                                    case 0:
                                        AttributeData.TexCoord_0.Add(vertTexCoord);
                                        break;
                                    case 1:
                                        AttributeData.TexCoord_1.Add(vertTexCoord);
                                        break;
                                    case 2:
                                        AttributeData.TexCoord_2.Add(vertTexCoord);
                                        break;
                                    case 3:
                                        AttributeData.TexCoord_3.Add(vertTexCoord);
                                        break;
                                    case 4:
                                        AttributeData.TexCoord_4.Add(vertTexCoord);
                                        break;
                                    case 5:
                                        AttributeData.TexCoord_5.Add(vertTexCoord);
                                        break;
                                    case 6:
                                        AttributeData.TexCoord_6.Add(vertTexCoord);
                                        break;
                                    case 7:
                                        AttributeData.TexCoord_7.Add(vertTexCoord);
                                        break;
                                }

                                vert.SetAttributeIndex(Enums.GXVertexAttribute.Tex0 + texNo, (uint)texCoordData.IndexOf(vertTexCoord));
                                break;
                        }
                    }

                    //triindices[vertIndex] = 1;
                    prim.Vertices.Add(vert);
                }

                pack.Primitives.Add(prim);
            }

            
            pack.MatrixIndices.Add(0);
            Packets.Add(pack);

            Bounds.GetBoundsValues(AttributeData.Positions);
        }

        public void ProcessVerticesWithWeights(Mesh mesh, VertexData vertData, Dictionary<string, int> boneNames, EVP1 envelopes, DRW1 partialWeight)
        {
            Packet pack = new Packet();

            //Primitive prim = new Primitive(Enums.GXPrimitiveType.Triangles);
            List<Enums.GXVertexAttribute> activeAttribs = Descriptor.GetActiveAttributes();
            AttributeData.SetAttributesFromList(activeAttribs);

            Console.WriteLine("Calculating triangle strips for Weighted");
            uint[] triindices = WeldVertices(mesh);
            TriStripper stripper = new TriStripper(triindices);
            List<PrimitiveBrawl> primlist = stripper.Strip();

            Console.WriteLine(String.Format("Done, {0} primitives", primlist.Count));
            
            int numMatrices = 0;
            // foreach (Face face in mesh.Faces)
            foreach (PrimitiveBrawl primbrawl in primlist) {
                //Primitive prim = new Primitive(Enums.GXPrimitiveType.TriangleStrip);
                Primitive prim = new Primitive((Enums.GXPrimitiveType)primbrawl.Type);
                List<Weight> packetWeights = new List<Weight>();

                foreach (int vertIndex in primbrawl.Indices) {
                    Weight vertWeight = new Weight();

                    // Get the weights for this tri's vertices
                    foreach (Assimp.Bone bone in mesh.Bones)
                    {
                        foreach (VertexWeight weight in bone.VertexWeights)
                        {
                            if (weight.VertexID == vertIndex)
                                vertWeight.AddWeight(weight.Weight, boneNames[bone.Name]);
                        }
                    }

                    if (!packetWeights.Contains(vertWeight))
                        numMatrices += vertWeight.WeightCount;

                    // There are too many matrices, we need to create a new packet
                    if (numMatrices > 10)
                    {
                        pack.Primitives.Add(prim);
                        Packets.Add(pack);

                        prim = new Primitive((Enums.GXPrimitiveType)primbrawl.Type);
                        pack = new Packet();

                        packetWeights.Clear();
                        numMatrices = 0;

                        packetWeights.Add(vertWeight);

                        if (!packetWeights.Contains(vertWeight))
                            numMatrices += vertWeight.WeightCount;
                    }
                    // Matrix count is below 10, we can continue using the current packet
                    else
                    {
                        if (!packetWeights.Contains(vertWeight))
                            packetWeights.Add(vertWeight);
                    }

                    //int[] vertexIndexArray = new int[] { vert1Index, vert2Index, vert3Index };
                    //Weight[] vertWeightArray = new Weight[] { vert1Weight, vert2Weight, vert3Weight };

                    Vertex vert = new Vertex();
                    //int vertIndex = vertexIndexArray[i];
                    Weight curWeight = vertWeight;

                    vert.SetWeight(curWeight);

                    foreach (Enums.GXVertexAttribute attrib in activeAttribs) {
                        switch (attrib) {
                            case Enums.GXVertexAttribute.PositionMatrixIdx:
                                int newMatrixIndex = -1;

                                if (curWeight.WeightCount == 1) {
                                    newMatrixIndex = partialWeight.MeshWeights.IndexOf(curWeight);
                                }
                                else {
                                    if (!envelopes.Weights.Contains(curWeight))
                                        envelopes.Weights.Add(curWeight);

                                    int envIndex = envelopes.Weights.IndexOf(curWeight);
                                    int drwIndex = partialWeight.MeshWeights.IndexOf(curWeight);

                                    newMatrixIndex = drwIndex;
                                    partialWeight.Indices[drwIndex] = envIndex;
                                }

                                if (!pack.MatrixIndices.Contains(newMatrixIndex))
                                    pack.MatrixIndices.Add(newMatrixIndex);

                                vert.SetAttributeIndex(Enums.GXVertexAttribute.PositionMatrixIdx, (uint)pack.MatrixIndices.IndexOf(newMatrixIndex));
                                break;
                            case Enums.GXVertexAttribute.Position:
                                List<Vector3> posData = (List<Vector3>)vertData.GetAttributeData(Enums.GXVertexAttribute.Position);
                                Vector3 vertPos = mesh.Vertices[vertIndex].ToOpenTKVector3();

                                if (curWeight.WeightCount == 1) {
                                    Matrix4 ibm = envelopes.InverseBindMatrices[curWeight.BoneIndices[0]];

                                    Vector3 transVec = Vector3.TransformPosition(vertPos, ibm);
                                    posData.Add(transVec);
                                    AttributeData.Positions.Add(transVec);
                                    vert.SetAttributeIndex(Enums.GXVertexAttribute.Position, (uint)posData.IndexOf(transVec));
                                }
                                else {
                                    AttributeData.Positions.Add(vertPos);

                                    vert.SetAttributeIndex(Enums.GXVertexAttribute.Position, (uint)posData.IndexOf(vertPos));
                                }
                                break;
                            case Enums.GXVertexAttribute.Normal:
                                List<Vector3> normData = (List<Vector3>)vertData.GetAttributeData(Enums.GXVertexAttribute.Normal);
                                Vector3 vertNrm = mesh.Normals[vertIndex].ToOpenTKVector3();
                                AttributeData.Normals.Add(vertNrm);

                                vert.SetAttributeIndex(Enums.GXVertexAttribute.Normal, (uint)normData.IndexOf(vertNrm));
                                break;
                            case Enums.GXVertexAttribute.Color0:
                            case Enums.GXVertexAttribute.Color1:
                                int colNo = (int)attrib - 11;
                                List<Color> colData = (List<Color>)vertData.GetAttributeData(Enums.GXVertexAttribute.Color0 + colNo);
                                Color vertCol = mesh.VertexColorChannels[colNo][vertIndex].ToSuperBMDColorRGBA();

                                if (colNo == 0)
                                    AttributeData.Color_0.Add(vertCol);
                                else
                                    AttributeData.Color_1.Add(vertCol);

                                vert.SetAttributeIndex(Enums.GXVertexAttribute.Color0 + colNo, (uint)colData.IndexOf(vertCol));
                                break;
                            case Enums.GXVertexAttribute.Tex0:
                            case Enums.GXVertexAttribute.Tex1:
                            case Enums.GXVertexAttribute.Tex2:
                            case Enums.GXVertexAttribute.Tex3:
                            case Enums.GXVertexAttribute.Tex4:
                            case Enums.GXVertexAttribute.Tex5:
                            case Enums.GXVertexAttribute.Tex6:
                            case Enums.GXVertexAttribute.Tex7:
                                int texNo = (int)attrib - 13;
                                List<Vector2> texCoordData = (List<Vector2>)vertData.GetAttributeData(Enums.GXVertexAttribute.Tex0 + texNo);
                                Vector2 vertTexCoord = mesh.TextureCoordinateChannels[texNo][vertIndex].ToOpenTKVector2();
                                vertTexCoord = new Vector2(vertTexCoord.X, 1.0f - vertTexCoord.Y);

                                switch (texNo) {
                                    case 0:
                                        AttributeData.TexCoord_0.Add(vertTexCoord);
                                        break;
                                    case 1:
                                        AttributeData.TexCoord_1.Add(vertTexCoord);
                                        break;
                                    case 2:
                                        AttributeData.TexCoord_2.Add(vertTexCoord);
                                        break;
                                    case 3:
                                        AttributeData.TexCoord_3.Add(vertTexCoord);
                                        break;
                                    case 4:
                                        AttributeData.TexCoord_4.Add(vertTexCoord);
                                        break;
                                    case 5:
                                        AttributeData.TexCoord_5.Add(vertTexCoord);
                                        break;
                                    case 6:
                                        AttributeData.TexCoord_6.Add(vertTexCoord);
                                        break;
                                    case 7:
                                        AttributeData.TexCoord_7.Add(vertTexCoord);
                                        break;
                                }

                                vert.SetAttributeIndex(Enums.GXVertexAttribute.Tex0 + texNo, (uint)texCoordData.IndexOf(vertTexCoord));
                                break;
                            }
                    }
                    prim.Vertices.Add(vert);
                }
                pack.Primitives.Add(prim);
            }

            
            Packets.Add(pack);
        }

        public void Write(EndianBinaryWriter writer)
        {
            writer.Write(MatrixType);
            writer.Write((sbyte)-1);
            writer.Write((short)Packets.Count);
            writer.Write((short)0); // Placeholder for descriptor offset
            writer.Write((short)0); // Placeholder for starting packet index
            writer.Write((short)0); // Placeholder for starting packet matrix index offset
            writer.Write((short)-1);
            Bounds.Write(writer);
        }
    }
}
