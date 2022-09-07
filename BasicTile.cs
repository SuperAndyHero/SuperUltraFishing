using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace SuperUltraFishing
{
    public struct BasicTile
    {
        public BasicTile() { }

        public bool Active = false;

        public ushort TileType = 0;

        public Vector2 TileFrame = Vector2.Zero;//used for frame important tiles only

        public BlockType BlockType = BlockType.Solid;

        public bool Collide = true;

        public byte Color = 0;

        //public int FrameFrontX = 0;
        //public int FrameFrontY = 0;
    }
}
