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

        public string Error = "";

        public Point16 LastWorldLocation = Point16.Zero;

        public int WaterDistanceFromTop = 0;

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
            !tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType];//may need Main.tileSolidTop[type]

        private bool WaterPassThough(Tile tile) =>
            !tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType];//may need NotActuallySolid[]

        private bool ValidLiquidTile(Tile tile, int LiquidID = LiquidID.Water) =>
            WaterPassThough(tile) && tile.LiquidType == LiquidID && tile.LiquidAmount > minLiquid;

        const int minLiquid = 32;
        const int maxLakeRectSize = 1000;
        const int surfaceTryDistance = 64;
        const int wallBuffer = 5;
        public Rectangle? FindWorldRect(Point16 start)//returns null if invalid, returns a rectangle if successful
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
                            surfaceOrigin = new Point16(start.X + xRoofOffset, (start.Y - i) + 1);
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
                {
                    Error = "Too deep or no valid surface";
                    return null;
                }

                bool lFoundWater = true;
                int lOff = 0;
                bool rFoundWater = true;
                int rOff = 0;
                bool dFoundWater = true;
                int dOff = 0;

                //dynamically finds the max area needed by checking for water blocks
                //later the main pool will be crawled
                //possibly other side pools may be as well, if this is the case connecting tunnels will be created
                for (int a = 0; a < maxLakeRectSize; a++)
                {
                    //checks left row
                    for (int l = (lFoundWater ? 0 : dOff); l < dOff + 2; l++)//only checks bottom block if no liquid was found
                    {
                        Tile tile = Main.tile[(surfaceOrigin.X - lOff) - 1, surfaceOrigin.Y + l];
                        if (ValidLiquidTile(tile))
                        {
                            //WorldGen.PlaceWall((surfaceOrigin.X - lOff) - 1, surfaceOrigin.Y + l, WallID.AmethystGemspark, true);
                            lFoundWater = true;
                            lOff++;
                            break;
                        }
                        else
                            lFoundWater = false;
                    }

                    //checks right row
                    for (int r = (rFoundWater ? 0 : dOff); r < dOff + 2; r++)//only checks bottom block if no liquid was found
                    {
                        Tile tile = Main.tile[(surfaceOrigin.X + rOff) + 1, surfaceOrigin.Y + r];
                        if (ValidLiquidTile(tile))
                        {
                            //WorldGen.PlaceWall((surfaceOrigin.X + rOff) + 1, surfaceOrigin.Y + r, WallID.EmeraldGemspark, true);
                            rFoundWater = true;
                            rOff++;
                            break;
                        }
                        else
                            rFoundWater = false;
                    }

                    //checks bottom row
                    if (dFoundWater)
                        for (int d = 0; d < lOff + rOff + 1; d++)
                        {
                            Tile tile = Main.tile[(surfaceOrigin.X - lOff) + d, surfaceOrigin.Y + dOff + 1];
                            if (ValidLiquidTile(tile))
                            {
                                //WorldGen.PlaceWall((surfaceOrigin.X - lOff) + d, surfaceOrigin.Y + dOff + 1, WallID.SapphireGemspark, true);
                                dFoundWater = true;
                                dOff++;
                                break;
                            }
                            else
                               dFoundWater = false;
                        }
                    else//only checks under each side bar if no water was found
                    {
                        Tile ltile = Main.tile[(surfaceOrigin.X - lOff) - 1, surfaceOrigin.Y + dOff + 2];
                        Tile rtile = Main.tile[(surfaceOrigin.X + rOff) + 1, surfaceOrigin.Y + dOff + 2];
                        if ((ValidLiquidTile(ltile)) ||
                            ValidLiquidTile(rtile))
                        {
                            //WorldGen.PlaceWall((surfaceOrigin.X - lOff) - 1, surfaceOrigin.Y + dOff + 2, WallID.RubyGemspark, true);
                            //WorldGen.PlaceWall((surfaceOrigin.X + rOff) - 1, surfaceOrigin.Y + dOff + 2, WallID.RubyGemsparkOff, true);
                            dOff++;
                            dFoundWater = true;
                        }
                    }

                    if(!lFoundWater && !rFoundWater && !dFoundWater)
                        break;
                }

                Rectangle waterRect = new Rectangle((surfaceOrigin.X - lOff) - wallBuffer, (surfaceOrigin.Y - 1) - wallBuffer, lOff + rOff + 1 + (wallBuffer * 2), dOff + 1 + (wallBuffer * 2));
                return waterRect;
            }
            else
            {
                Error = "Invalid Origin";
                return null;
            }
        }

        public List<(Point16 center, int total)> WaterBodyScan(Rectangle worldArea)
        {

            HashSet<Point> checkedPoints = new HashSet<Point>();

            List<(Point16 center, int total)> waterBodyList = new List<(Point16 center, int total)>();

            for (int i = 0; i < worldArea.Width; i++)
            {
                for (int j = 0; j < worldArea.Height; j++)
                {
                    Point position = new Point(worldArea.X + i, worldArea.Y + j);
                    if (ValidLiquidTile(Main.tile[position]) && !checkedPoints.Contains(position))
                        waterBodyList.Add(CrawlWaterBody(position, checkedPoints));
                }
            }


            return waterBodyList;
        }

        public const int MaxCheckedWaterTiles = 8000;
        public (Point16 center, int total) CrawlWaterBody(Point startPosition, HashSet<Point> checkedPoints)
        {
            List<Point> pointAverageList = new List<Point>();//later this will be used to average each point
            Queue<Point> checkSurroundQueue = new Queue<Point>();//queue of tiles to have their surroundings checked

            //starts out by adding start position to each list since its a known correct tile
            pointAverageList.Add(startPosition);
            checkSurroundQueue.Enqueue(startPosition);
            checkedPoints.Add(startPosition);

            int totalWater = 0;//could use the pointAverageList count instead

            for (int i = 0; i < MaxCheckedWaterTiles; i++)
            {
                if (checkSurroundQueue.Count == 0)
                    break;

                Point pos = checkSurroundQueue.Dequeue();

                CheckAddWaterTile(pos + new Point(0, -1));
                CheckAddWaterTile(pos + new Point(1, 0));
                CheckAddWaterTile(pos + new Point(0, 1));
                CheckAddWaterTile(pos + new Point(-1, 0));
            }

            void CheckAddWaterTile(Point pos)
            {
                if(!checkedPoints.Contains(pos) && ValidLiquidTile(Main.tile[pos]))
                {
                    checkedPoints.Add(pos);
                    checkSurroundQueue.Enqueue(pos);
                    pointAverageList.Add(pos);
                    totalWater++;
                }
            }

            if (checkSurroundQueue.Count != 0)
                Error = "Body of water too large, result may not be complete";

            Point average = Point.Zero;

            foreach (Point nxtPos in pointAverageList)
                average += nxtPos;

            int count = pointAverageList.Count();
            average /= new Point(count, count);

            return (average.ToVector2().ToPoint16(), totalWater);
        }

        public void CaptureWorldArea(Rectangle worldArea)
        {
            AreaSizeX = worldArea.Width + 20;
            AreaSizeZ = worldArea.Width + 20;
            AreaSizeY = worldArea.Height + 20;

            AreaArray = new BasicTile[AreaSizeX, AreaSizeY, AreaSizeZ];



            Point16 centerOffset = new Point16((worldArea.Width / 2), (worldArea.Height / 2));
            Vector3 center = new Vector3((AreaSizeX / 2) - centerOffset.X, (AreaSizeY / 2) - centerOffset.Y, AreaSizeZ / 2);

            FastNoise.FastNoise noise = new FastNoise.FastNoise((int)Main.GameUpdateCount);

            //for (int i = 0; i < worldArea.Width; i++)
            //{
            //    for (int j = 0; j < worldArea.Height; j++)
            //    {
            //        int xoffset = ((int)center.X + i) - centerOffset.X;
            //        int yoffset = ((int)center.Y + j) - centerOffset.Y;
            //        int zoffset = (int)center.Z;

            //        Tile vanillaTile = Main.tile[worldArea.X + i, (worldArea.Height + worldArea.Y) - j];

            //        AreaArray[xoffset, yoffset, zoffset] = new BasicTile()
            //        {
            //            Active = vanillaTile.HasTile,
            //            TileType = vanillaTile.TileType,
            //            BlockType = vanillaTile.BlockType,
            //            Color = vanillaTile.TileColor
            //        };
            //    }
            //}

            for (int i = 0; i < AreaSizeX; i++)
            {
                for (int j = 0; j < AreaSizeY; j++)
                {
                    for (int k = 0; k < AreaSizeZ; k++)
                    {
                        int scale = 3;
                        float noiseVal = noise.GetCubicFractal(i * scale, j * scale, k * scale) * 25;
                        AreaArray[i, j, k] = new BasicTile()
                        {
                            Active = noiseVal > 0.5f,
                            TileType = noiseVal < 0.75f ? TileID.Obsidian : TileID.MythrilBrick,
                        };
                    }
                }
            }

            AreaArray[(int)center.X, (int)center.Y, (int)center.Z] = new BasicTile()
            {
                Active = true,
                TileType = TileID.LunarOre
            };
        }

        public bool GenerateWorld(Point16 worldLocation)
        {
            Error = "";
            WorldGenerated = false;
            LastWorldLocation = worldLocation;

            Rectangle? worldrect = FindWorldRect(worldLocation);

            if (worldrect == null)
                return false;

            //List<(Point16 center, int total)> waterBodyList = WaterBodyScan(worldrect.Value);

            CaptureWorldArea(worldrect.Value);

            WorldGenerated = true;
            return true;
        }
    }
}
