using Microsoft.Xna.Framework;
using SuperUltraFishing.World;
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

        public bool Active = false;//is there a tile here

        public ushort TileType = 0;

        public Vector2 TileFrame = Vector2.Zero;//only used for frame important tiles

        public BlockType BlockType = BlockType.Solid;

        public bool Collide = true;

        public byte PaintColor = 0;

        public BlockModelType Model = BlockModelType.Cube;//this could be removed and gotten as the mesh is generated

        public enum BlockModelType : byte
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
            if (GameWorld.CrossTile.Contains(TileType))
                Model = BlockModelType.Cross;
            else if (GameWorld.FourSidedTiles.Contains(TileType))
                Model = BlockModelType.FourSidedCube;
            else if (Main.tileFrameImportant[TileType])//TODO extruded is not used, and 2 sided can likely be removed later once extruded works (not even used right now anyway)
                Model = BlockModelType.TwoSidedCube;
            else if (!Main.tileBlockLight[TileType] && !Main.tileLighted[TileType])//unsure if this lighted check will break stuff
                Model = BlockModelType.CubeTransparent;
            else
                Model = BlockModelType.Cube;
        }
    }
}
