using System;
using System.Collections.Generic;
using System.Linq;
using GameFormatReader.Common;
using SuperBMDLib.Animation.Enums;

namespace SuperBMDLib.Animation
{
    public class BCK : J3DJointAnimation
    {
        protected override string FileMagic => "J3D1bck1";
        protected override string SectionMagic => "ANK1";

        public BCK(EndianBinaryReader reader)
            : base(reader)
        {
        }

        public BCK(Assimp.Animation src_anim, List<Rigging.Bone> bone_list, float threshold)
            : base(src_anim, bone_list, threshold)
        {
        }

        protected override byte GetAngleFraction()
        {
            float maxValue = 0.0f;
            foreach (Track track in Tracks)
            {
                maxValue = Math.Max(maxValue, track.Rotation.X.Select(k => Math.Abs(k.Value)).Max());
                maxValue = Math.Max(maxValue, track.Rotation.Y.Select(k => Math.Abs(k.Value)).Max());
                maxValue = Math.Max(maxValue, track.Rotation.Z.Select(k => Math.Abs(k.Value)).Max());
            }

            if (maxValue < 180.0f)
            {
                return 0;
            }

            return (byte)(maxValue / 180);
        }

        protected override Keyframe[] ReadChannel(EndianBinaryReader reader, float[] data)
        {
            ushort keyCount = reader.ReadUInt16();
            ushort dataIndex = reader.ReadUInt16();
            TangentMode tangentMode = (TangentMode)reader.ReadUInt16();

            Keyframe[] keyframes = new Keyframe[keyCount];
            if (keyCount == 1)
            {
                keyframes[0].Time = 0;
                keyframes[0].Value = data[dataIndex];
                return keyframes;
            }

            for (int i = 0; i < keyCount; i++)
            {
                int currentIndex = dataIndex;
                if (tangentMode == TangentMode.Symmetric)
                {
                    currentIndex += 3 * i;
                }
                else
                {
                    currentIndex += 4 * i;
                }

                keyframes[i].Time = data[currentIndex];
                keyframes[i].Value = data[currentIndex + 1];
                keyframes[i].InTangent = data[currentIndex + 2];

                if (tangentMode == TangentMode.Piecewise)
                {
                    keyframes[i].OutTangent = data[currentIndex + 3];
                }
            }

            return keyframes;
        }

        protected override Keyframe[] ReadChannel(EndianBinaryReader reader, short[] data)
        {
            ushort keyCount = reader.ReadUInt16();
            ushort dataIndex = reader.ReadUInt16();
            TangentMode tangentMode = (TangentMode)reader.ReadUInt16();

            Keyframe[] keyframes = new Keyframe[keyCount];
            if (keyCount == 1)
            {
                keyframes[0].Time = 0;
                keyframes[0].Value = data[dataIndex] * RotationScale;
                return keyframes;
            }

            for (int i = 0; i < keyCount; i++)
            {
                int currentIndex = dataIndex;
                if (tangentMode == TangentMode.Symmetric)
                {
                    currentIndex += 3 * i;
                }
                else
                {
                    currentIndex += 4 * i;
                }

                keyframes[i].Time = data[currentIndex];
                keyframes[i].Value = data[currentIndex + 1] * RotationScale;
                keyframes[i].InTangent = data[currentIndex + 2] * RotationScale;

                if (tangentMode == TangentMode.Piecewise)
                {
                    keyframes[i].OutTangent = data[currentIndex + 3] * RotationScale;
                }
            }

            return keyframes;
        }

        protected override void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<float> data)
        {
            TangentMode tangentMode = GetTangentMode(keys);

            List<float> channelSequence = new List<float>();
            foreach (Keyframe key in keys)
            {
                channelSequence.Add(key.Time);
                channelSequence.Add(key.Value);

                if (key.InTangent != null)
                {
                    channelSequence.Add((float)key.InTangent);
                }

                if (key.OutTangent != null && tangentMode == TangentMode.Piecewise)
                {
                    channelSequence.Add((float)key.OutTangent);
                }
            }

            if (keys.Length == 1)
            {
                channelSequence = new List<float>() { keys[0].Value };
            }

            int dataIndex = FindSequenceIndex(data, channelSequence);

            writer.Write((ushort)keys.Length);
            writer.Write((ushort)dataIndex);
            writer.Write((ushort)tangentMode);
        }

        protected override void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<short> data)
        {
            TangentMode tangentMode = GetTangentMode(keys);

            List<short> channelSequence = new List<short>();
            foreach (Keyframe key in keys)
            {
                channelSequence.Add((short)key.Time);
                channelSequence.Add((short)(key.Value / RotationScale));

                if (key.InTangent != null)
                {
                    channelSequence.Add((short)(key.InTangent / RotationScale));
                }

                if (key.OutTangent != null && tangentMode == TangentMode.Piecewise)
                {
                    channelSequence.Add((short)(key.OutTangent / RotationScale));
                }
            }

            if (keys.Length == 1)
            {
                channelSequence = new List<short>() { (short)(keys[0].Value / RotationScale) };
            }

            int dataIndex = FindSequenceIndex(data, channelSequence);

            writer.Write((ushort)keys.Length);
            writer.Write((ushort)dataIndex);
            writer.Write((ushort)tangentMode);
        }

        private TangentMode GetTangentMode(Keyframe[] keys)
        {
            foreach (Keyframe key in keys)
            {
                if (key.OutTangent != null && key.InTangent != key.OutTangent)
                {
                    return TangentMode.Piecewise;
                }
            }

            return TangentMode.Symmetric;
        }
    }
}
