using GameFormatReader.Common;
using System;
using System.Collections.Generic;

namespace SuperBMDLib.Animation
{
    public class BCA : J3DJointAnimation
    {
        protected override string FileMagic => "J3D1bca1";
        protected override string SectionMagic => "ANF1";

        public BCA(EndianBinaryReader reader) 
            : base(reader) 
        {
        }

        public BCA(Assimp.Animation src_anim, List<Rigging.Bone> bone_list, float threshold) 
            : base(src_anim, bone_list, threshold)
        {
        }

        protected override Keyframe[] ReadChannel(EndianBinaryReader reader, float[] data) 
        {
            ushort keyCount  = reader.ReadUInt16();
            ushort dataIndex = reader.ReadUInt16();

            Keyframe[] keyframes = new Keyframe[keyCount];
            if (keyCount == 1)
            {
                keyframes[0].Time  = 0;
                keyframes[0].Value = data[dataIndex];
                return keyframes;
            }

            for (int i = 0; i < keyCount; i++)
            {
                keyframes[i].Time  = data[dataIndex];
                keyframes[i].Value = data[dataIndex + i];
            }

            return keyframes;
        }

        protected override Keyframe[] ReadChannel(EndianBinaryReader reader, short[] data) 
        {
            ushort keyCount  = reader.ReadUInt16();
            ushort dataIndex = reader.ReadUInt16();

            Keyframe[] keyframes = new Keyframe[keyCount];
            if (keyCount == 1)
            {
                keyframes[0].Time  = 0;
                keyframes[0].Value = data[dataIndex] * RotationScale;
                return keyframes;
            }

            for (int i = 0; i < keyCount; i++)
            {
                keyframes[i].Time  = data[dataIndex];
                keyframes[i].Value = data[dataIndex + i] * RotationScale;
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

            writer.Write((ushort)keys.Length);
            writer.Write((ushort)dataIndex);
        }

        protected override void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<short> data) 
        {
            List<short> channelSequence = new List<short>();
            foreach (Keyframe key in keys)
            {
                channelSequence.Add((short)(key.Value / RotationScale));
            }

            int dataIndex = FindSequenceIndex(data, channelSequence);

            writer.Write((ushort)keys.Length);
            writer.Write((ushort)dataIndex);
        }
    }
}
