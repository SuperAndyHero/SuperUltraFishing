using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using static Terraria.ModLoader.ModContent;

namespace SuperUltraFishing
{
    internal class World : ModSystem
    {
        public int AreaSizeX = 32;
        public int AreaSizeY = 32;
        public int AreaSizeZ = 32;

        public BasicTile[,,] AreaArray = new BasicTile[1, 1, 1];
        public bool WorldGenerated = false;

        public void PlaceTile(ushort type, int x, int y, int z)
        {
            AreaArray[x, y, z].TileType = type;
            AreaArray[x, y, z].Active = true;
        }

        public void CaptureWorldArea(Point16 point1, Point16 point2)
        {
            
        }

        public void GenerateWorld()
        {
            if(AreaSizeX == AreaArray.GetLength(0) || AreaSizeY == AreaArray.GetLength(1) || AreaSizeZ == AreaArray.GetLength(2))
                AreaArray = new BasicTile[AreaSizeX, AreaSizeY, AreaSizeZ];

            for (int x = 0; x < AreaSizeX; x++)
            {
                for (int y = 0; y < AreaSizeY; y++)
                {
                    for (int z = 0; z < AreaSizeZ; z++)
                    {
                        //if(x == 0 || y == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1)
                        //    AreaArray[x, y, z].Active = true;
                        //else
                        //    AreaArray[x, y, z].Active = false;
                        //AreaArray[x, y, z].Active = (x % 5 == 0 || y % 3 == 0 || z % 7 == 0);
                        //AreaArray[x, y, z].Active = false;
                    }
                }
            }

            WorldGenerated = true;
        }
    }
}
