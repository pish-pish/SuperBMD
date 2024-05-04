using System;
using System.Collections.Generic;
using GameFormatReader.Common;
using SuperBMDLib.Rigging;
using OpenTK;
using SuperBMD.Util;
using System.IO;

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
            return base.GetAngleFraction();
        }

        protected override Keyframe[] ReadFloatChannel(EndianBinaryReader reader, float[] data)
        {
            return new Keyframe[] { };
        }

        protected override Keyframe[] ReadShortChannel(EndianBinaryReader reader, short[] data)
        {
            return new Keyframe[] { };
        }

        protected override void WriteFloatChannel(EndianBinaryWriter writer, Keyframe[] keys, List<float> data)
        {

        }

        protected override void WriteShortChannel(EndianBinaryWriter writer, Keyframe[] keys, List<short> data)
        {

        }
    }
}
