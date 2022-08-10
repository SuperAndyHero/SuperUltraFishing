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

        public int TileType = 0;

        public BlockType BlockType = BlockType.Solid;

        public byte Color = 0;

        //public int FrameFrontX = 0;
        //public int FrameFrontY = 0;
    }
}
