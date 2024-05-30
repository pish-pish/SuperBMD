using GameFormatReader.Common;
using OpenTK;
using SuperBMD.Util;
using SuperBMDLib.Animation.Enums;
using SuperBMDLib.Rigging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Messaging;

namespace SuperBMDLib.Animation
{ 
    public class J3DJointAnimation
    {
        public string Name { get; private set; }
        public LoopMode LoopMode;
        public float RotationScale;
        public short Duration;

        protected virtual string FileMagic { get; }
        protected virtual string SectionMagic { get; }

        public Track[] Tracks;

        public J3DJointAnimation(Assimp.Animation src_anim, List<Bone> bone_list, float threshold=0)
        {
            Name          = src_anim.Name;
            LoopMode      = LoopMode.Loop;
            Duration      = (short)(src_anim.DurationInTicks * (30.0f / src_anim.TicksPerSecond));
            Console.WriteLine(src_anim.TicksPerSecond);

            Tracks = new Track[bone_list.Count];

            for (int i = 0; i < bone_list.Count; i++)
            {
                Assimp.NodeAnimationChannel node = src_anim.NodeAnimationChannels.Find(x => x.NodeName == bone_list[i].Name);

                if (node == null)
                {
                    Tracks[i] = Track.Identity(bone_list[i].TransformationMatrix, Duration);
                }
                else
                {
                    Tracks[i] = new Track()
                    {
                        Scale       = GenerateAxis(node.ScalingKeys, threshold),
                        Rotation    = GenerateAxis(node.RotationKeys, threshold),
                        Translation = GenerateAxis(node.PositionKeys, threshold),
                    };
                }
            }

            byte angleFrac = GetAngleFraction();
            if (angleFrac == 0xFF)
            {
                angleFrac = 0;
            }
            RotationScale = (float)Math.Pow(2.0f, angleFrac) * (180.0f / 32768.0f);
        }

        public J3DJointAnimation(EndianBinaryReader reader) 
        {
            string magic = new string(reader.ReadChars(FileMagic.Length));
            Debug.Assert(magic.Equals(FileMagic), "File Magic is invalid.");

            reader.ReadUInt32(); // filesize

            uint sectionCount = reader.ReadUInt32();
            Debug.Assert(sectionCount == 1, "More than 1 data sections; cannot proceed.");

            reader.Skip(16); // skip svn/svr data and sound section offset

            ReadSection(reader);
        }

        protected virtual Keyframe[] ReadChannel(EndianBinaryReader reader, float[] data) => new Keyframe[] { }; 

        protected virtual Keyframe[] ReadChannel(EndianBinaryReader reader, short[] data) => new Keyframe[] { };

        protected virtual void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<float> data) { }

        protected virtual void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<short> data) { }

        protected virtual byte GetAngleFraction() => 0xFF;

        private Axis GenerateAxis(List<Assimp.VectorKey> keys, float threshold)
        {
            List<Keyframe> xKeys = new List<Keyframe>();
            List<Keyframe> yKeys = new List<Keyframe>();
            List<Keyframe> zKeys = new List<Keyframe>();

            for (int i = 0; i < keys.Count; i++)
            {
                xKeys.Add(new Keyframe { 
                    Time      = (float)keys[i].Time,
                    Value     = keys[i].Value.X,
                    InTangent = 0,
                });

                yKeys.Add(new Keyframe
                {
                    Time = (float)keys[i].Time,
                    Value = keys[i].Value.Y,
                    InTangent = 0,
                });

                zKeys.Add(new Keyframe
                {
                    Time = (float)keys[i].Time,
                    Value = keys[i].Value.Z,
                    InTangent = 0,
                });
            }

            if (threshold > 0.0f)
            {
                CleanAxis(xKeys, threshold);
                CleanAxis(yKeys, threshold);
                CleanAxis(zKeys, threshold);
            }

            return new Axis
            {
                X = xKeys.ToArray(),
                Y = yKeys.ToArray(),
                Z = zKeys.ToArray(),
            };
        }

        private Axis GenerateAxis(List<Assimp.QuaternionKey> keys, float threshold)
        {
            List<Keyframe> xKeys = new List<Keyframe>();
            List<Keyframe> yKeys = new List<Keyframe>();
            List<Keyframe> zKeys = new List<Keyframe>();

            for (int i = 0; i < keys.Count; i++)
            {
                Assimp.QuaternionKey current_key = keys[i];
                Quaternion value = new Quaternion(current_key.Value.X, current_key.Value.Y, current_key.Value.Z, current_key.Value.W);
                Vector3 quat_as_vec = QuaternionExtensions.ToEulerAngles(value);

                xKeys.Add(new Keyframe
                {
                    Time = (float)current_key.Time,
                    Value = quat_as_vec.X,
                    InTangent = 0,
                });

                yKeys.Add(new Keyframe
                {
                    Time = (float)current_key.Time,
                    Value = quat_as_vec.Y,
                    InTangent = 0,
                });

                zKeys.Add(new Keyframe
                {
                    Time = (float)current_key.Time,
                    Value = quat_as_vec.Z,
                    InTangent = 0,
                });
            }

            if (threshold > 0.0f)
            {
                CleanAxis(xKeys, threshold);
                CleanAxis(yKeys, threshold);
                CleanAxis(zKeys, threshold);
            }

            return new Axis
            {
                X = xKeys.ToArray(),
                Y = yKeys.ToArray(),
                Z = zKeys.ToArray(),
            };
        }

        /// <summary> Cleans keyframe axis by slopes between frames under a threshold </summary>
        /// <param name="keys"> list of keyframes </param>
        /// <param name="threshold"> threshold for cleaning </param>
        private void CleanAxis(List<Keyframe> keys, float threshold)
        {
            for (int i = keys.Count - 1; i - 2 > 0; i--)
            {
                int currentFrame    = i;
                Keyframe currentKey = keys[currentFrame];

                int prevFrame    = currentFrame - 1;
                Keyframe prevKey = keys[prevFrame];

                float currentSlope = (currentKey.Value - prevKey.Value) / (currentFrame - prevFrame);

                currentFrame = prevFrame;
                currentKey   = prevKey;

                prevFrame--;
                prevKey = keys[prevFrame];

                float prevSlope = (currentKey.Value - prevKey.Value) / (currentFrame - prevFrame);

                if (Math.Abs(currentSlope - prevSlope) < threshold)
                {
                    keys.RemoveAt(currentFrame);
                }
                else
                {
                    return;
                }
            }
        }

        public static int FindSequenceIndex<T>(List<T> dataList, List<T> sequenceList)
        {
            int matchup = 0, start = -1;

            bool started = false;

            for (int i = 0; i < dataList.Count; i++)
            {
                if (!dataList[i].Equals(sequenceList[matchup]))
                {
                    matchup = 0;
                    start = -1;
                    started = false;
                    continue;
                }

                if (!started)
                {
                    start = i;
                    started = true;
                }

                matchup += 1;
                if (matchup == sequenceList.Count)
                {
                    return start;
                }
            }

            start = dataList.Count;
            dataList.AddRange(sequenceList);

            return start;
        }

        #region Reading

        private void ReadSection(EndianBinaryReader reader)
        {
            // section header
            string magic = new string(reader.ReadChars(SectionMagic.Length));
            Debug.Assert(magic.Equals(SectionMagic), "Data section Magic is invalid.");
            reader.ReadUInt32(); // section size

            LoopMode = (LoopMode)reader.ReadByte();

            byte angleFrac = reader.ReadByte();
            Console.WriteLine($"Angle Frac: {angleFrac}");
            if (angleFrac == 0xFF)
            {
                angleFrac = 0;
            }
            RotationScale = (float)Math.Pow(2.0f, angleFrac) * (180.0f / 32768.0f);

            Duration = (short)reader.ReadUInt16();
            Console.WriteLine($"Duration: {Duration}");

            // counts for tracks and component data
            ushort trackCount       = reader.ReadUInt16();
            Console.WriteLine($"Track Count: {trackCount}");

            ushort scaleCount       = reader.ReadUInt16();
            Console.WriteLine($"Scale Count: {scaleCount}");

            ushort rotationCount    = reader.ReadUInt16();
            Console.WriteLine($"Rotation Count: {rotationCount}");

            ushort translationCount = reader.ReadUInt16();
            Console.WriteLine($"Translation Count: {translationCount}");

            // data offsets with an extra 4 bytes added to skip padding between sections
            uint tracksOffset       = reader.ReadUInt32() + 32;
            uint scalesOffset       = reader.ReadUInt32() + 32;
            uint rotationsOffset    = reader.ReadUInt32() + 32;
            uint translationsOffset = reader.ReadUInt32() + 32;

            float[] scaleData       = ReadFloatTable(scalesOffset, scaleCount, reader);
            short[] rotationData    = ReadShortTable(rotationsOffset, rotationCount, reader);
            float[] translationData = ReadFloatTable(translationsOffset, translationCount, reader);

            Tracks = new Track[trackCount];
            reader.BaseStream.Seek(tracksOffset, SeekOrigin.Begin);

            for (int i = 0; i < trackCount; i++)
            {
                Tracks[i].Scale.X       = ReadChannel(reader, scaleData);
                Tracks[i].Rotation.X    = ReadChannel(reader, rotationData);
                Tracks[i].Translation.X = ReadChannel(reader, translationData);

                Tracks[i].Scale.Y       = ReadChannel(reader, scaleData);
                Tracks[i].Rotation.Y    = ReadChannel(reader, rotationData);
                Tracks[i].Translation.Y = ReadChannel(reader, translationData);

                Tracks[i].Scale.Z       = ReadChannel(reader, scaleData);
                Tracks[i].Rotation.Z    = ReadChannel(reader, rotationData);
                Tracks[i].Translation.Z = ReadChannel(reader, translationData);
            }
        }

        private float[] ReadFloatTable(uint offset, ushort count, EndianBinaryReader reader)
        {
            float[] floats = new float[count];

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);

            for (int i = 0; i < count; i++)
            {
                floats[i] = reader.ReadSingle();
            }

            return floats;
        }

        private short[] ReadShortTable(uint offset, ushort count, EndianBinaryReader reader)
        {
            short[] shorts = new short[count];

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);

            for (int i = 0; i < count; i++)
            {
                shorts[i] = reader.ReadInt16();
            }

            return shorts;
        }

        #endregion

        #region Writing

        public void Write(EndianBinaryWriter writer)
        {
            writer.Write(FileMagic.ToCharArray()); // magic

            long sizeOffset = writer.BaseStream.Length;
            writer.Write(0); // placeholder for filesize

            writer.Write(1); // section count-- only ever 1

            // These are placeholder for SVN that were never used
            writer.Write(-1);
            writer.Write(-1);
            writer.Write(-1);

            // This spot, however, was used for hacking sound data into the animation.
            // It's the offset to the start of the sound data. Unsupported for now.
            writer.Write(-1);

            WriteSection(writer);

            writer.BaseStream.Seek(sizeOffset, SeekOrigin.Begin);
            writer.Write((uint)writer.BaseStream.Length); // total filesize
            writer.BaseStream.Seek(0, SeekOrigin.End);
        }

        private void WriteSection(EndianBinaryWriter writer)
        {
            int sectionStart = (int)writer.BaseStream.Length;

            byte[] keyframeData = WriteKeyframeData(out int scaleCount, out int rotCount, 
                out int transCount, out int scaleOffset, out int rotOffset, out int transOffset);

            writer.Write(SectionMagic.ToCharArray()); // magic

            long sizeOffset = writer.BaseStream.Length;
            writer.Write(0); // placeholder for section size

            writer.Write((byte)LoopMode);
            Console.WriteLine($"\nLoop Mode: {LoopMode}");

            writer.Write(GetAngleFraction());
            Console.WriteLine($"Angle Frac: {GetAngleFraction()}");

            writer.Write((ushort)Duration);
            Console.WriteLine($"Duration: {Duration}");

            writer.Write((ushort)Tracks.Length);
            Console.WriteLine($"Tracks Count: {Tracks.Length}");

            writer.Write((ushort)scaleCount);
            Console.WriteLine($"Scales Count: {scaleCount}");

            writer.Write((ushort)rotCount);
            Console.WriteLine($"Rotations Count: {rotCount}");

            writer.Write((ushort)transCount);
            Console.WriteLine($"Translations Count: {transCount}");

            // this offset will always be at 0x40
            const int tracksOffset = 0x40;
            writer.Write(tracksOffset);
            writer.Write(scaleOffset + tracksOffset);
            writer.Write(rotOffset + tracksOffset);
            writer.Write(transOffset + tracksOffset);

            Util.StreamUtility.PadStreamWithString(writer, "Animation made with love and care :)", 32);

            writer.Write(keyframeData);

            Util.StreamUtility.PadStreamWithString(writer, "Animation made with love and care :)", 32);

            writer.BaseStream.Seek(sizeOffset, SeekOrigin.Begin);
            writer.Write((uint)(writer.BaseStream.Length - sectionStart));
            writer.BaseStream.Seek(0, SeekOrigin.End);
        }

        private byte[] WriteKeyframeData(out int scaleCount, out int rotCount, 
            out int transCount, out int scaleOffset, out int rotOffset, out int transOffset)
        {
            List<float> scaleData       = new List<float>();
            List<short> rotationData    = new List<short>();
            List<float> translationData = new List<float>();
            byte[] keyframeData;

            using (MemoryStream stream = new MemoryStream())
            {
                EndianBinaryWriter writer = new EndianBinaryWriter(stream, Endian.Big);

                foreach (Track track in Tracks)
                {
                    WriteChannel(writer, track.Scale.X, scaleData);
                    WriteChannel(writer, track.Rotation.X, rotationData);
                    WriteChannel(writer, track.Translation.X, translationData);

                    WriteChannel(writer, track.Scale.Y, scaleData);
                    WriteChannel(writer, track.Rotation.Y, rotationData);
                    WriteChannel(writer, track.Translation.Y, translationData);

                    WriteChannel(writer, track.Scale.Z, scaleData);
                    WriteChannel(writer, track.Rotation.Z, rotationData);
                    WriteChannel(writer, track.Translation.Z, translationData);
                }

                Util.StreamUtility.PadStreamWithString(writer, "Animation made with love and care :)", 32);

                scaleOffset = (int)writer.BaseStream.Position;
                foreach (float f in scaleData)
                    writer.Write(f);

                Util.StreamUtility.PadStreamWithString(writer, "Animation made with love and care :)", 32);

                rotOffset = (int)writer.BaseStream.Position;
                foreach (short s in rotationData)
                    writer.Write(s);

                Util.StreamUtility.PadStreamWithString(writer, "Animation made with love and care :)", 32);

                transOffset = (int)writer.BaseStream.Position;
                foreach (float f in translationData)
                    writer.Write(f);

                keyframeData = stream.ToArray();
            }

            scaleCount = scaleData.Count;
            rotCount   = rotationData.Count;
            transCount = translationData.Count;

            return keyframeData;
        }

        #endregion
    }
}
