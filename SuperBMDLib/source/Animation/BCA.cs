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
            return new Keyframe[] { }; 
        }

        protected override Keyframe[] ReadChannel(EndianBinaryReader reader, short[] data) 
        { 
            return new Keyframe[] { }; 
        }

        protected override void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<float> data) 
        {

        }

        protected override void WriteChannel(EndianBinaryWriter writer, Keyframe[] keys, List<short> data) 
        {

        }
    }
}
