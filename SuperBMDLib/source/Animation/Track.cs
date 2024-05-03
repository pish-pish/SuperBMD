using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using SuperBMD.Util;

namespace SuperBMDLib.Animation
{
    public enum TangentMode
    {
        Symmetric,
        Piecewise
    }

    public struct Keyframe
    {
        public float Time;
        public float Key;
        public float InTangent;
        public float OutTangent;
    }

    public struct Axis
    {
        public Keyframe[] X;
        public Keyframe[] Y;
        public Keyframe[] Z;
    }

    public struct Track
    {
        public Axis Translation;
        public Axis Rotation;
        public Axis Scale;

        public bool IsIdentity;

        public static Track Identity(Matrix4 Transform, float MaxTime)
        {
            Track ident_track = new Track();
            Quaternion XRot = Quaternion.FromAxisAngle(Vector3.UnitX, 0.0f);

            ident_track.IsIdentity = true;
            Vector3 Translation = Transform.ExtractTranslation();

            ident_track.Translation = new Axis()
            {
                X = new Keyframe[] { new Keyframe() { InTangent = 0, Key = Translation.X, OutTangent = 0 } },
                Y = new Keyframe[] { new Keyframe() { InTangent = 0, Key = Translation.Y, OutTangent = 0, Time = 0} },
                Z = new Keyframe[] { new Keyframe() { InTangent = 0, Key = Translation.Z, OutTangent = 0, Time = 0} },
            };

            Quaternion Rotation = Transform.ExtractRotation();
            Vector3 Rot_Vec = QuaternionExtensions.ToEulerAngles(Rotation);

            ident_track.Rotation = new Axis()
            {
                X = new Keyframe[] { new Keyframe() { InTangent = 0, Key = Rot_Vec.X, OutTangent = 0, Time = 0} },
                Y = new Keyframe[] { new Keyframe() { InTangent = 0, Key = Rot_Vec.Y, OutTangent = 0, Time = 0} },
                Z = new Keyframe[] { new Keyframe() { InTangent = 0, Key = Rot_Vec.Z, OutTangent = 0, Time = 0} },
            };

            Vector3 Scale = Transform.ExtractScale();

            ident_track.Scale = new Axis()
            {
                X = new Keyframe[] { new Keyframe() { InTangent = 0, Key = 1, OutTangent = 0, Time = 0} },
                Y = new Keyframe[] { new Keyframe() { InTangent = 0, Key = 1, OutTangent = 0, Time = 0} },
                Z = new Keyframe[] { new Keyframe() { InTangent = 0, Key = 1, OutTangent = 0, Time = 0} },
            };

            return ident_track;
        }
    }
}
