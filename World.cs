using Microsoft.Xna.Framework;
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

        public BasicTile[,,] AreaArray = null;
        public bool WorldGenerated = false;

        public void PlaceTile(ushort type, int x, int y, int z)
        {
            AreaArray[x, y, z].TileType = type;
            AreaArray[x, y, z].Active = true;
        }

        public void RemoveWorld()//unload world
        {
            AreaArray = null;
            WorldGenerated = false;
        }

        public void CaptureWorldArea(Point16 point1, Point16 point2)
        {
            int width = Math.Abs(point1.X - point2.X);
            int height = Math.Abs(point1.Y - point2.Y);
            Vector3 center = new Vector3((AreaSizeX / 2) - (width / 2), (AreaSizeY / 2) - (height / 2), AreaSizeZ / 2);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int xoffset = (int)center.X + i;
                    int yoffset = (int)center.Y + j;
                    int zoffset = (int)center.Z;

                    Tile vanillaTile = Main.tile[point1.X + i, point2.Y - j];
                    AreaArray[xoffset, yoffset, zoffset] = new BasicTile()
                    {
                        Active = vanillaTile.HasTile,
                        TileType = vanillaTile.TileType,
                        BlockType = vanillaTile.BlockType,
                        Color = vanillaTile.TileColor
                    };
                }
            }
        }

        public void GenerateWorld()
        {
            AreaArray = new BasicTile[AreaSizeX, AreaSizeY, AreaSizeZ];
            //if (AreaArray == null || 
            //    AreaSizeX != AreaArray.GetLength(0) ||  AreaSizeY != AreaArray.GetLength(1) ||  AreaSizeZ != AreaArray.GetLength(2))
            //        AreaArray = new BasicTile[AreaSizeX, AreaSizeY, AreaSizeZ];

            //for (int x = 0; x < AreaSizeX; x++)
            //{
            //    for (int y = 0; y < AreaSizeY; y++)
            //    {
            //        for (int z = 0; z < AreaSizeZ; z++)
            //        {
                        //AreaArray[x, y, z].Active = false;
                        //if (x == 0 || y == 0 || z == 0 || x == AreaSizeX - 1 || z == AreaSizeZ - 1)
                        //    AreaArray[x, y, z].Active = true;
                        //else
                        //    AreaArray[x, y, z].Active = false;
                        //AreaArray[x, y, z].Active = (x % 5 == 0 || y % 3 == 0 || z % 7 == 0);
                        //AreaArray[x, y, z].Active = false;
            //        }
            //    }
            //}

            WorldGenerated = true;
        }
    }
}
