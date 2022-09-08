using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
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

        public BlockModel Model = BlockModel.Cube;

        public enum BlockModel
        {
            Cube = 0,
            Cross = 1,
            CubeTransparent = 2,
            Extruded = 3,
            TwoSidedCube = 4,
            FourSidedCube = 5
        }


        public void GetTileModel()
        {
            if (World.CrossTile.Contains(TileType))
                Model = BlockModel.Cross;
            else if (World.FourSidedTiles.Contains(TileType))
                Model = BlockModel.FourSidedCube;
            else if (Main.tileFrameImportant[TileType])
                Model = BlockModel.TwoSidedCube;
            else if (!Main.tileBlockLight[TileType] && !Main.tileLighted[TileType])
                Model = BlockModel.CubeTransparent;
            else
                Model = BlockModel.Cube;
        }
    }
}
