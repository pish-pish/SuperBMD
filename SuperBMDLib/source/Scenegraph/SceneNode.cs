using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperBMDLib.Scenegraph.Enums;
using GameFormatReader.Common;
using Newtonsoft.Json;

namespace SuperBMDLib.Scenegraph
{
    public class SceneNode
    {
        [JsonIgnore]
        public SceneNode Parent { get; set; }

        public NodeType Type { get; set; }
        public int Index { get; set; }
        public List<SceneNode> Children { get; set; }
        
        public SceneNode() {
            Parent = null;
            Type = NodeType.Joint;
            Index = 0;
            Children = new List<SceneNode>();

        }
        
        public SceneNode(EndianBinaryReader reader, SceneNode parent)
        {
            Children = new List<SceneNode>();
            Parent = parent;

            Type = (NodeType)reader.ReadInt16();
            Index = reader.ReadInt16();
        }

        public SceneNode(NodeType type, int index, SceneNode parent)
        {
            Type = type;
            Index = index;
            Parent = parent;

            if (Parent != null)
                Parent.Children.Add(this);

            Children = new List<SceneNode>();
        }

        public void SetParent(SceneNode parent) {
            Parent = parent;
        }

        public override string ToString()
        {
            return $"{ Type } : { Index }";
        }
    }
}
