using GameFormatReader.Common;
using System;
using System.Diagnostics;
using System.IO;

namespace SuperBMD
{
    public class J3DHeader
    {
        protected virtual string FileMagic { get; }
        protected virtual int SectionCount { get; private set; }

        public int FileSize { get; private set; }

        private int mSizeOffset;

        public J3DHeader() { }

        public J3DHeader(EndianBinaryReader reader)
        {
            string magic = new string(reader.ReadChars(FileMagic.Length));
            Debug.Assert(magic.Equals(FileMagic), "File Magic is invalid.");

            FileSize = reader.ReadInt32();
            SectionCount = reader.ReadInt32();

            // Subversion (SVR) data is unused.
            reader.Skip(16);
        }

        public J3DHeader(EndianBinaryReader reader, params string[] validMagics)
        {
            foreach (string filemagic in validMagics)
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                string magic = new string(reader.ReadChars(filemagic.Length));

                if (magic.Equals(filemagic))
                {
                    FileMagic = filemagic;
                    break;
                }
            }

            if (FileMagic == null)
            {
                throw new Exception("File Magic is invalid.");
            }

            FileSize = reader.ReadInt32();
            SectionCount = reader.ReadInt32();

            // Subversion (SVR) data is unused.
            reader.Skip(16);
        }

        public virtual void Write(EndianBinaryWriter writer)
        {
            writer.Write(FileMagic.ToCharArray());

            mSizeOffset = (int)writer.BaseStream.Position;
            writer.Write(0); // placeholder for file size
            writer.Write(SectionCount);

            // padding for SVR data
            writer.Write("SuperBMD - Gamma".PadRight(16).ToCharArray());
        }

        public void WriteSize(EndianBinaryWriter writer)
        {
            writer.Seek(mSizeOffset, SeekOrigin.Begin);
            FileSize = (int)writer.BaseStream.Length;
            writer.Write(FileSize);
            writer.Seek(0, SeekOrigin.End);
        }
    }
}
