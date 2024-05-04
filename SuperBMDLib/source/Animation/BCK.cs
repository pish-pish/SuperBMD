using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GameFormatReader.Common;
using OpenTK.Audio.OpenAL;

namespace SuperBMDLib.Animation
{
    public class BCK : J3DJointAnimation
    {
        public BCK(EndianBinaryReader reader) : base(reader)
        {
            FileMagic = "J3D1bck1";
            SectionMagic = "ANK1";
        }

        public BCK(Assimp.Animation src_anim, List<Rigging.Bone> bone_list) : base(src_anim, bone_list)
        {
            FileMagic = "J3D1bck1";
            SectionMagic = "ANK1";
        }

        protected override sbyte GetAngleFraction()
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

            return (sbyte)(maxValue / 180);
        }

        protected override Keyframe[] ReadChannel(EndianBinaryReader reader, float[] data)
        {
            ushort keyCount         = reader.ReadUInt16();
            ushort dataIndex        = reader.ReadUInt16();
            TangentMode tangentMode = (TangentMode)reader.ReadUInt16();

            Keyframe[] keyframes = new Keyframe[keyCount];
            if (keyCount == 1)
            {
                keyframes[0].Time  = 0;
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

                keyframes[i].Time      = data[currentIndex];
                keyframes[i].Value     = data[currentIndex + 1];
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
            ushort keyCount         = reader.ReadUInt16();
            ushort dataIndex        = reader.ReadUInt16();
            TangentMode tangentMode = (TangentMode)reader.ReadUInt16();

            Keyframe[] keyframes = new Keyframe[keyCount];
            if (keyCount == 1)
            {
                keyframes[0].Time  = 0;
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

                keyframes[i].Time      = data[currentIndex];
                keyframes[i].Value     = data[currentIndex + 1] * RotationScale;
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
            for (int i = 0; i < keys.Length; i++)
            {
                channelSequence.Append(keys[i].Time);
                channelSequence.Append(keys[i].Value);

                if (keys[i].InTangent != null)
                {
                    channelSequence.Append((float)keys[i].InTangent);
                }

                if (keys[i].OutTangent != null && tangentMode == TangentMode.Piecewise)
                {
                    channelSequence.Append((float)keys[i].OutTangent);
                }
            }

            if (keys.Length == 1)
            {
                channelSequence = new List<float>() { keys[0].Value };
            }

            int dataIndex = FindSequenceIndex(data, channelSequence);
            data.AddRange(channelSequence);

            writer.Write((ushort)data.Count);
            writer.Write((ushort)dataIndex);
            writer.Write((ushort)tangentMode);
        }

        protected override void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<short> data)
        {
            TangentMode tangentMode = GetTangentMode(keys);

            List<short> channelSequence = new List<short>();
            for (int i = 0; i < keys.Length; i++)
            {
                channelSequence.Append((short)keys[i].Time);
                channelSequence.Append((short)(keys[i].Value / RotationScale));

                if (keys[i].InTangent != null)
                {
                    channelSequence.Append((short)(keys[i].InTangent / RotationScale));
                }

                if (keys[i].OutTangent != null && tangentMode == TangentMode.Piecewise)
                {
                    channelSequence.Append((short)(keys[i].OutTangent / RotationScale));
                }
            }

            if (keys.Length == 1)
            {
                channelSequence = new List<short>() { (short)(keys[0].Value / RotationScale) };
            }

            int dataIndex = FindSequenceIndex(data, channelSequence);
            data.AddRange(channelSequence);

            writer.Write((ushort)data.Count);
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
