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
        public BasicTile[,,] AreaArray = new BasicTile[16, 16, 16];

        public void PlaceTile(ushort type, int x, int y, int z)
        {
            AreaArray[x, y, z].TileType = type;
            AreaArray[x, y, z].Active = true;
        }

        public void GenerateWorld()
        {
            int sizeX = AreaArray.GetLength(0);
            int sizeY = AreaArray.GetLength(1);
            int sizeZ = AreaArray.GetLength(2);
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        //if(x == 0 || y == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1)
                        //    AreaArray[x, y, z].Active = true;
                        //else
                        //    AreaArray[x, y, z].Active = false;
                        AreaArray[x, y, z].Active = (x % 5 == 0 || y % 3 == 0 || z % 7 == 0);
                        AreaArray[x, y, z].Active = false;
                    }
                }
            }
        }
    }
}
