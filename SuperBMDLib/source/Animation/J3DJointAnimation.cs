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
        public byte RotationFrac;
        public short Duration;

        public Track[] Tracks;
    }
}
