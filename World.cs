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

        private bool EntityPassThough(Tile tile) =>
            !tile.HasTile || !Main.tileSolid[tile.TileType];//may need Main.tileSolidTop[type]

        private bool WaterPassThough(Tile tile) =>
            !tile.HasTile || !Main.tileSolid[tile.TileType];//may need NotActuallySolid[]

        const int minLiquid = 32;
        //const int maxLakeSize = 400;
        const int surfaceTryDistance = 64; 
        public (BasicTile tile, bool check)[,] CrawlWorld(Point16 start)//returns null if invalid, returns a 2D array if successful
        {
            Tile startTile = Main.tile[start.X, start.Y];

            //liquid type check will be replaced later with different fishing types
            //checks if origin is valid
            if (startTile.LiquidAmount > minLiquid && startTile.LiquidType == LiquidID.Water)
            {
                Point16 surfaceOrigin = start;//start of search and surface location
                bool atSurface = false;

                //distance to check until surface
                //current offset to the side in case origin in under a tile
                int xRoofOffset = 0;

                //starts going from origin up to surface, moving slightly to the side it theres a ceiling
                for (int i = 0; i < surfaceTryDistance; i++)
                {
                    Tile aboveTile = Main.tile[start.X + xRoofOffset, start.Y - i];
                    //checks for any projecile passable block (water blocking does not matter)
                    if (EntityPassThough(aboveTile))//if above does not have a tile, check for liquid. 
                    {
                        if (aboveTile.LiquidAmount < minLiquid)//if there is no liquid break loop, else keep trying
                        {
                            surfaceOrigin = new Point16(start.X + xRoofOffset, start.Y - i);
                            atSurface = true;
                            break;
                        }
                    }
                    else//if above has tile, check for open spaces nearby
                    {
                        int xRoof1 = xRoofOffset - 2;
                        int xRoof2 = xRoofOffset + 3;//sets new bounds
                        for (int h = xRoof1; h < xRoof2; h++)//checks in a 5 block area around the previous offset for open tiles
                        {
                            xRoofOffset = h;//sets a new offset, if a valid tile is found this offset is kept
                            aboveTile = Main.tile[start.X + xRoofOffset, start.Y - i];
                            //checks for any passable block (water blocking does not matter)
                            if (EntityPassThough(aboveTile))
                            {
                                if (aboveTile.LiquidAmount < minLiquid)
                                {
                                    surfaceOrigin = new Point16(start.X + xRoofOffset, start.Y - i);
                                    atSurface = true;
                                }

                                //break either way, since if there is a valid gap we keep checking, or if we found the surface we break the outer loop
                                break;
                            }
                        }
                        if (atSurface)
                            break;
                    }
                }

                if (!atSurface)
                    return null;//may return null and/or give too deep message

                bool lFoundWater = true;
                int lOff = 0;
                bool rFoundWater = true;
                int rOff = 0;
                bool dFoundWater = true;
                int dOff = 0;

                //dynamically finds the max area needed by checking for water blocks
                //later the main pool will be crawled
                //possibly other side pools may be as well, if this is the case connecting tunnels will be created
                for (int a = 0; a < 1000; a++)
                {
                    //checks left row
                    for (int l = lFoundWater ? 0 : dOff; l < dOff + 1; l++)//only checks bottom block if no liquid was found
                    {
                        Tile tile = Main.tile[surfaceOrigin.X - lOff, surfaceOrigin.Y + l];
                        if (WaterPassThough(tile) && tile.LiquidType == LiquidID.Water && tile.LiquidAmount > minLiquid)
                        {
                            lOff++;
                            lFoundWater = true;
                            break;
                        }
                        else
                            lFoundWater = false;
                    }

                    //checks right row
                    for (int r = rFoundWater ? 0 : dOff; r < dOff + 1; r++)//only checks bottom block if no liquid was found
                    {
                        Tile tile = Main.tile[surfaceOrigin.X + rOff, surfaceOrigin.Y + r];
                        if (WaterPassThough(tile) && tile.LiquidType == LiquidID.Water && tile.LiquidAmount > minLiquid)
                        {
                            rOff++;
                            rFoundWater = true;
                            break;
                        }
                        else
                            lFoundWater = false;
                    }

                    //checks bottom row
                    if (dFoundWater)
                        for (int d = 0; d < lOff + rOff + 1; d++)
                        {
                            Tile tile = Main.tile[(surfaceOrigin.X - lOff) + d, surfaceOrigin.Y + dOff];
                            if (WaterPassThough(tile) && tile.LiquidType == LiquidID.Water && tile.LiquidAmount > minLiquid)
                            {
                                dOff++;
                                dFoundWater = true;
                                break;
                            }
                            else
                               dFoundWater = false;
                        }
                    else//only checks under each side bar if no water was found
                    {
                        Tile ltile = Main.tile[(surfaceOrigin.X - lOff), surfaceOrigin.Y + dOff + 1];
                        Tile rtile = Main.tile[(surfaceOrigin.X + rOff), surfaceOrigin.Y + dOff + 1];
                        if((WaterPassThough(ltile) && ltile.LiquidType == LiquidID.Water && ltile.LiquidAmount > minLiquid) ||
                            WaterPassThough(rtile) && rtile.LiquidType == LiquidID.Water && rtile.LiquidAmount > minLiquid)
                        {
                            dOff++;
                            dFoundWater = true;
                            break;
                        }
                    }
                }

                    (BasicTile tile, bool check)[,] array = new (BasicTile tile, bool check)[maxLakeSize, maxLakeSize];
                int xArrayCenter = maxLakeSize / 2;
                HashSet<Point16> 
            }
            else
                return null;//invalid origin
        }

        public void CaptureWorldArea(Point16 point1, Point16 point2)
        {
            int width = Math.Abs(point1.X - point2.X);
            int height = Math.Abs(point1.Y - point2.Y);
            Vector3 center = new Vector3((AreaSizeX / 2) - (width / 2), (AreaSizeY / 2) - (height / 2), AreaSizeZ / 2);
            for (int i = 0; i < width; i++)
            {
                bool rowHasTile = false;
                bool hasgap = false;
                Point16 firstTile = Point16.Zero;
                Point16 secondTile = Point16.Zero;

                for (int j = 0; j < height; j++)
                {
                    int xoffset = (int)center.X + i;
                    int yoffset = (int)center.Y + j;
                    int zoffset = (int)center.Z;

                    Tile vanillaTile = Main.tile[point1.X + i, point2.Y - j];

                    if (vanillaTile.HasTile)
                        rowHasTile = true;
                    else
                        hasgap = true;

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
