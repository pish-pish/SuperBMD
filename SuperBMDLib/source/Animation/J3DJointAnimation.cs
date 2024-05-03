using GameFormatReader.Common;
using OpenTK;
using SuperBMD.Util;
using SuperBMDLib.Rigging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public byte RotationScale;
        public short Duration;

        protected string Magic;

        public Track[] Tracks;

        public J3DJointAnimation(Assimp.Animation src_anim, List<Bone> bone_list)
        {
            Name = src_anim.Name;
            LoopMode = LoopMode.Loop;
            RotationFrac = 0;
            Duration = (short)(src_anim.DurationInTicks * 30.0f);

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
            Magic = new string(reader.ReadChars(8));
            reader.ReadInt32(); // filesize
            reader.ReadInt32(); // section count
            reader.Skip(16); 
        }

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

                axis.X[i].Key  = value.X;
                axis.X[i].Time = (float)current_key.Time;

                axis.Y[i].Key  = value.Y;
                axis.Y[i].Time = (float)current_key.Time;

                axis.Z[i].Key  = value.Z;
                axis.Z[i].Time = (float)current_key.Time;
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

                axis.X[i].Key  = quat_as_vec.X;
                axis.X[i].Time = (float)current_key.Time;

                axis.Y[i].Key  = quat_as_vec.Y;
                axis.Y[i].Time = (float)current_key.Time;

                axis.Z[i].Key  = quat_as_vec.Z;
                axis.Z[i].Time = (float)current_key.Time;
            }

            return axis;
        }
    }
}
