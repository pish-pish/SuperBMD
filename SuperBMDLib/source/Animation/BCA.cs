using GameFormatReader.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperBMDLib.Animation
{
    public class BCA : J3DJointAnimation
    {
        public BCA(Assimp.Animation src_anim, List<Rigging.Bone> bone_list) : base(src_anim, bone_list)
        {
            FileMagic = "J3D1bca1";
            SectionMagic = "ANF1";
        }

        public BCA(EndianBinaryReader reader) : base(reader)
        {
            FileMagic    = "J3D1bca1";
            SectionMagic = "ANF1";
        }

        protected override Keyframe[] ReadChannel(EndianBinaryReader reader, float[] data) 
        {
            ushort keyCount  = reader.ReadUInt16();
            ushort dataIndex = reader.ReadUInt16();

            Keyframe[] keyframes = new Keyframe[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                keyframes[i].Time  = data[dataIndex];
                keyframes[i].Value = data[dataIndex + 1];
            }

            return keyframes;
        }

        protected override Keyframe[] ReadChannel(EndianBinaryReader reader, short[] data) 
        {
            ushort keyCount  = reader.ReadUInt16();
            ushort dataIndex = reader.ReadUInt16();

            Keyframe[] keyframes = new Keyframe[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                keyframes[i].Time  = data[dataIndex];
                keyframes[i].Value = data[dataIndex + 1] * RotationScale;
            }

            return keyframes;
        }

        protected override void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<float> data) 
        {
            List<float> channelSequence = new List<float>();
            foreach (Keyframe key in keys)
            {
                channelSequence.Add(key.Value);
            }

            int dataIndex = FindSequenceIndex(data, channelSequence);
            data.AddRange(channelSequence);

            writer.Write((ushort)data.Count);
            writer.Write((ushort)dataIndex);
        }

        protected override void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<short> data) 
        {
            List<short> channelSequence = new List<short>();
            foreach (Keyframe key in keys)
            {
                channelSequence.Add((short)(key.Value * RotationScale));
            }

            int dataIndex = FindSequenceIndex(data, channelSequence);
            data.AddRange(channelSequence);

            writer.Write((ushort)data.Count);
            writer.Write((ushort)dataIndex);
        }
    }
}
