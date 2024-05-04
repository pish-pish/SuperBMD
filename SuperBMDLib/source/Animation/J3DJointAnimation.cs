using GameFormatReader.Common;
using OpenTK;
using SuperBMD.Util;
using SuperBMDLib.Rigging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SuperBMDLib.Animation
{ 
    public enum LoopMode
    {
        Once,
        OnceReset,
        Loop,
        MirroredOnce,
        MirroredLoop
    }

    public class J3DJointAnimation
    {
        public string Name { get; private set; }
        public LoopMode LoopMode;
        public float RotationScale;
        public ushort Duration;

        protected string FileMagic;
        protected string SectionMagic;

        public Track[] Tracks;

        public J3DJointAnimation(Assimp.Animation src_anim, List<Bone> bone_list)
        {
            Name = src_anim.Name;
            LoopMode = LoopMode.Loop;
            RotationScale = 180.0f / 32768.0f;
            Duration = (ushort)(src_anim.DurationInTicks * 30.0f);

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
                        Translation = GenerateTrack(node.PositionKeys, bone_list[i]),
                        Rotation    = GenerateRotationTrack(node.RotationKeys, bone_list[i]),
                        Scale       = GenerateTrack(node.ScalingKeys, bone_list[i]),
                    };
                }
            }
        }

        public J3DJointAnimation(EndianBinaryReader reader) 
        {
            string magic = new string(reader.ReadChars(FileMagic.Length));
            Debug.Assert(magic.Equals(FileMagic), "File Magic is invalid.");

            reader.ReadUInt32(); // filesize

            uint sectionCount = reader.ReadUInt32();
            Debug.Assert(sectionCount == 1);

            reader.Skip(16); // skip svn/svr data and sound section offset

            ReadSection(reader);
        }

        protected virtual Keyframe[] ReadChannel(EndianBinaryReader reader, float[] data) { return new Keyframe[] {}; }

        protected virtual Keyframe[] ReadChannel(EndianBinaryReader reader, short[] data) { return new Keyframe[] {}; }

        protected virtual void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<float> data) { }

        protected virtual void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<short> data) { }

        protected virtual sbyte GetAngleFraction() { return -1; }

        private Axis GenerateTrack(List<Assimp.VectorKey> keys, Bone bone)
        {
            Axis axis = new Axis()
            {
                X = new Keyframe[keys.Count],
                Y = new Keyframe[keys.Count],
                Z = new Keyframe[keys.Count],
            };

            for (int i = 0; i < keys.Count; i++)
            {
                Assimp.VectorKey current_key = keys[i];
                Vector3 value = new Vector3(current_key.Value.X, current_key.Value.Y, current_key.Value.Z);

                axis.X[i].Time  = (float)current_key.Time;
                axis.X[i].Value = value.X;

                axis.Y[i].Time  = (float)current_key.Time;
                axis.Y[i].Value = value.Y;

                axis.Z[i].Time  = (float)current_key.Time;
                axis.Z[i].Value = value.Z;
            }

            return axis;
        }

        private Axis GenerateRotationTrack(List<Assimp.QuaternionKey> keys, Bone bone)
        {
            Axis axis = new Axis()
            {
                X = new Keyframe[keys.Count],
                Y = new Keyframe[keys.Count],
                Z = new Keyframe[keys.Count],
            };

            for (int i = 0; i < keys.Count; i++)
            {
                Assimp.QuaternionKey current_key = keys[i];
                Quaternion value = new Quaternion(current_key.Value.X, current_key.Value.Y, current_key.Value.Z, current_key.Value.W);
                Vector3 quat_as_vec = QuaternionExtensions.ToEulerAngles(value);

                axis.X[i].Time  = (float)current_key.Time;
                axis.X[i].Value = quat_as_vec.X;

                axis.Y[i].Time  = (float)current_key.Time;
                axis.Y[i].Value = quat_as_vec.Y;

                axis.Z[i].Time  = (float)current_key.Time;
                axis.Z[i].Value = quat_as_vec.Z;
            }

            return axis;
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

                matchup++;
                if (matchup == sequenceList.Count)
                {
                    return start;
                }
            }

            return dataList.Count;
        }

        #region Reading

        private void ReadSection(EndianBinaryReader reader)
        {
            // section header
            string magic = new string(reader.ReadChars(SectionMagic.Length));
            Debug.Assert(magic.Equals(SectionMagic), "Data section Magic is invalid.");
            reader.ReadUInt32(); // section size

            LoopMode = (LoopMode)reader.ReadByte();

            sbyte angleFrac = reader.ReadSByte();
            if (angleFrac == -1)
            {
                angleFrac = 0;
            }
            RotationScale = (float)Math.Pow(2.0f, angleFrac) * (180.0f / 32768.0f);

            Duration = reader.ReadUInt16();

            // counts for tracks and component data
            ushort trackCount       = reader.ReadUInt16();
            ushort scaleCount       = reader.ReadUInt16();
            ushort rotationCount    = reader.ReadUInt16();
            ushort translationCount = reader.ReadUInt16();

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

            long sizeOffset = writer.BaseStream.Position;
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
            writer.Write((int)writer.BaseStream.Length); // total filesize
            writer.BaseStream.Seek(0, SeekOrigin.End);
        }

        private void WriteSection(EndianBinaryWriter writer)
        {
            long sectionStart = writer.BaseStream.Position;

            const int tracksOffset = 0x40; // known placement of tracks offset

            byte[] keyframeData = WriteKeyframeData(tracksOffset, out int scaleCount, out int rotCount, 
                out int transCount, out int scaleOffset, out int rotOffset, out int transOffset);

            writer.Write(SectionMagic.ToCharArray()); // magic

            long sizeOffset = writer.BaseStream.Position;
            writer.Write(0); // placeholder for section size

            writer.Write((byte)LoopMode);
            writer.Write(GetAngleFraction());
            writer.Write(Duration);

            writer.Write((ushort)Tracks.Length);
            writer.Write((ushort)scaleCount);
            writer.Write((ushort)rotCount);
            writer.Write((ushort)transCount);

            writer.Write(tracksOffset);
            writer.Write(scaleOffset);
            writer.Write(rotOffset);
            writer.Write(transOffset);

            Util.StreamUtility.PadStreamWithString(writer, "Animation made with love and care :)", 32);

            writer.Write(keyframeData);

            Util.StreamUtility.PadStreamWithString(writer, "Animation made with love and care :)", 32);

            writer.BaseStream.Seek(sizeOffset, SeekOrigin.Begin);
            writer.Write((int)writer.BaseStream.Length - sectionStart);
            writer.BaseStream.Seek(0, SeekOrigin.End);
        }

        private byte[] WriteKeyframeData(int tracksOffset, out int scaleCount, out int rotCount, 
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

                scaleOffset = (int)(writer.BaseStream.Position + tracksOffset);
                foreach (float f in scaleData)
                    writer.Write(f);

                Util.StreamUtility.PadStreamWithString(writer, "Animation made with love and care :)", 32);

                rotOffset = (int)(writer.BaseStream.Position + tracksOffset);
                foreach (short s in rotationData)
                    writer.Write(s);

                Util.StreamUtility.PadStreamWithString(writer, "Animation made with love and care :)", 32);

                transOffset = (int)(writer.BaseStream.Position + tracksOffset);
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
