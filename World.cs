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


        public static HashSet<ushort> FourSidedTiles;

        //public HashSet<ushort> FourSidedTilesBL;
        public static HashSet<ushort> CrossTile;

        public override void Load()
        {
            CrossTile = new HashSet<ushort>
            {
                TileID.Saplings,
                TileID.Bottles,
                TileID.FireflyinaBottle,
                TileID.LavaflyinaBottle,
                TileID.LightningBuginaBottle,
                TileID.SoulBottles,
                TileID.Chairs,
                TileID.Candles,
                TileID.Presents,
                TileID.HangingLanterns,
                TileID.WaterCandle,
                TileID.Books,
                TileID.ImmatureHerbs,
                TileID.MatureHerbs,
                TileID.BloomingHerbs,
                //TileID.Torches,   //sticks to walls, specal case
                TileID.Banners,
                TileID.Lampposts,
                TileID.Lamps,
                //TileID.Crystals,  //sticks to walls
                TileID.PressurePlates,
                TileID.WeightedPressurePlate,
                TileID.HolidayLights,
                TileID.Stalactite,
                TileID.GemSaplings,
                //TileID.ExposedGems,   //sticks to walls
                //TileID.LongMoss,  //sticks to walls
                TileID.Plants,
                TileID.Plants2,
                TileID.HallowedPlants,
                TileID.HallowedPlants2,
                TileID.CorruptPlants,
                TileID.CrimsonPlants,
                TileID.JunglePlants,
                TileID.JunglePlants2,
                TileID.DyePlants,
                TileID.MushroomPlants,
                TileID.PottedPlants1,
                TileID.PottedPlants2,
                TileID.PottedLavaPlants,
                TileID.PottedCrystalPlants,
                TileID.OasisPlants,
                //TileID.Rope,  //ropes are checked via tileRope instead
                TileID.VineFlowers,
                TileID.Vines,
                TileID.CrimsonVines,
                TileID.HallowedVines,
                TileID.JungleVines,
                TileID.MushroomVines,
                324,    //seashells
                TileID.VanityTreeSakuraSaplings,
                TileID.VanityTreeWillowSaplings,
                TileID.SeaOats,
                TileID.Cattail,
                TileID.LilyPad,
                185,    //small piles, needs a check for only tiles with a style of 11 or below
                TileID.WaterDrip,//these 4 are here so they dont become solid tiles
                TileID.SandDrip,
                TileID.HoneyDrip,
                TileID.LavaDrip
            };

            FourSidedTiles = new HashSet<ushort>
            {
                TileID.MetalBars,
                TileID.Trees,
                TileID.TreeDiamond,
                TileID.TreeEmerald,
                TileID.TreeAmber,
                TileID.TreeAmethyst,
                TileID.TreeRuby,
                TileID.TreeSapphire,
                TileID.TreeTopaz,
                TileID.MushroomTrees,
                TileID.PalmTree,
                TileID.PineTree,
                TileID.VanityTreeSakura,
                TileID.VanityTreeYellowWillow
            };

            //FourSidedTilesBL = new HashSet<ushort>
            //{
            //    TileID.CorruptThorns,
            //    TileID.CrimsonThorns,
            //    TileID.JungleThorns,
            //    TileID.Cactus
            //};
        }

        public override void Unload()
        {
            base.Unload();
        }

        public void PlaceTile(ushort type, int x, int y, int z)
        {
            AreaArray[x, y, z].TileType = type;
            AreaArray[x, y, z].Active = true;
        }

        //unused and imcomplete
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
        public const int wallBuffer = 5;//maybe make a seperate surface buffer
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

        public (Dictionary<Point, float> TilePosDist, List<(Point16 center, int waterCount)>) WaterBodyScan(Rectangle worldArea)
        {

            HashSet<Point> checkedPoints = new HashSet<Point>();

            Dictionary<Point, float> CombinedDict = new Dictionary<Point, float>();
            List<(Point16 center, int count)> waterBodyList = new List<(Point16 center, int count)>();

            for (int i = 0; i < worldArea.Width; i++)
            {
                for (int j = 0; j < worldArea.Height; j++)
                {
                    Point position = new Point(worldArea.X + i, worldArea.Y + j);
                    if (ValidLiquidTile(Main.tile[position]) && !checkedPoints.Contains(position))
                    {
                        (List<KeyValuePair<Point, float>> WaterPosDist, (Point16 center, int waterCount)) = CrawlWaterBody(position, checkedPoints, worldArea);
                        WaterPosDist.ForEach(x => CombinedDict[x.Key] = x.Value);
                        waterBodyList.Add((center, WaterPosDist.Count()));
                    }
                }
            }

            return (CombinedDict, waterBodyList);
        }

        public const int MaxCheckedWaterTiles = 8000;
        public const int surroundTilesForGround = 12;

        public (List<KeyValuePair<Point, float>> TilePosDist, (Point16 center, int waterCount)) CrawlWaterBody(Point startPosition, HashSet<Point> checkedPoints, Rectangle worldArea)
        {
            //location of every tile that gets it's grounded tile distance calculated, this includes grounded tiles
            List<Point> tileLocationList = new List<Point>();

            //every tile connected to ground, used to calculate the distance from
            List<Point> groundedLocationList = new List<Point>();

            //the distance of every water tile from the closest grounded tile
            List<KeyValuePair<Point, float>> tileGroundDistance = new List<KeyValuePair<Point, float>>();//this gets returned

            Queue<Point> checkSurroundQueue = new Queue<Point>();//queue of tiles to have their surroundings checked

            Point average = Point.Zero;

            //starts out by adding start position to each list since its a known correct tile
            tileLocationList.Add(startPosition);
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
                if(!checkedPoints.Contains(pos))
                {
                    if (ValidLiquidTile(Main.tile[pos]))
                    {
                        checkedPoints.Add(pos);
                        checkSurroundQueue.Enqueue(pos);
                        average += pos;
                        totalWater++;
                    }
                    else
                    {
                        //only adds to ground list if a tile is considered part of the ground
                        //this could attach a bool instead if needed
                        int totalSurround = 0;
                        CheckTileLine(new Point(0, -1));
                        CheckTileLine(new Point(1, 0));
                        CheckTileLine(new Point(0, 1));
                        CheckTileLine(new Point(-1, 0));

                        void CheckTileLine(Point offset)
                        {
                            for (int u = 0; u < surroundTilesForGround; u++)
                            {
                                Point offsetPoint = pos + (offset * new Point(u, u));
                                //if this tile is outside the total bounding box immediately consider the base tile part of the ground
                                if (!worldArea.Contains(offsetPoint))
                                    totalSurround += surroundTilesForGround;
                                //adds to total if tile is water blocking, else it stops
                                if (totalSurround >= surroundTilesForGround || WaterPassThough(Main.tile[offsetPoint]))
                                    break;
                                else
                                    totalSurround++;
                            }
                        }

                        if(totalSurround >= surroundTilesForGround)
                            groundedLocationList.Add(pos);
                    }

                    tileLocationList.Add(pos);//grounded, floating, and water tiles are all added to this list
                }
            }

            if (checkSurroundQueue.Count != 0)
                Error = "Body of water too large, result may not be complete";

            foreach (Point waterPos in tileLocationList)
            {
                float nearestPoint = float.MaxValue;
                foreach(Point groundPos in groundedLocationList)
                {
                    float distance = Vector2.Distance(waterPos.ToVector2(), groundPos.ToVector2());
                    if (distance < nearestPoint)
                        nearestPoint = distance;
                }
                tileGroundDistance.Add(new KeyValuePair<Point, float>(waterPos, nearestPoint));
            }

            average /= new Point(totalWater, totalWater);

            return (tileGroundDistance, (average.ToVector2().ToPoint16(), totalWater));
        }

        //temp name
        public void CaptureWorldArea(Rectangle worldArea)
        {
            const int extraSizeY = 6;
            const int extraSizeZ = 6;

            AreaSizeX = worldArea.Width;
            AreaSizeY = worldArea.Height + extraSizeY;
            AreaSizeZ = worldArea.Width + extraSizeZ;

            AreaArray = new BasicTile[AreaSizeX, AreaSizeY, AreaSizeZ];



            Point16 centerOffset = new Point16((worldArea.Width / 2), (worldArea.Height / 2));
            Vector3 areaCenter = new Vector3((AreaSizeX / 2), (AreaSizeY / 2), AreaSizeZ / 2);
            Vector3 worldAreaCorner = areaCenter - new Vector3(centerOffset.X, centerOffset.Y + (extraSizeY / 2), 0);

            FastNoise.FastNoise noise = new FastNoise.FastNoise((int)(Main.GameUpdateCount % int.MaxValue));

            (Dictionary<Point, float> TilePositonDistance, List<(Point16 center, int waterCount)> WaterBodyCenterCount) = WaterBodyScan(worldArea);

            float MaxVal = 0;
            foreach(var pair in TilePositonDistance)
            {
                if (pair.Value > MaxVal)
                    MaxVal = pair.Value;

            }

            //TODO: make sure the pool is round in any pool size
            //TODO: increase cubic noise amplitude when poolsize is larger
            //Later todo: find out why water passable tiles set set as the last valid tile
            //Later todo: set Z distance to max when edge of world has been met

            ushort lastTileType = 1;
            for (int j = 0; j < worldArea.Height; j++)
            {
                int yoffset = ((int)worldAreaCorner.Y + j);
                int yWorld = (worldArea.Height + worldArea.Y) - j;

                int aboveWaterLeftDist = -1;
                int aboveWaterRightDist = -1;

                if (worldArea.Height - j < wallBuffer + 1)
                {
                    for (int l = wallBuffer / 2; l < wallBuffer; l++)
                        if (!WaterPassThough(Main.tile[worldArea.X + l, yWorld]))
                        {
                            aboveWaterLeftDist = l;
                            lastTileType = Main.tile[worldArea.X + l, yWorld].TileType;
                        }

                    for (int r = wallBuffer / 2; r < wallBuffer + 1; r++)
                        if (!WaterPassThough(Main.tile[(worldArea.X + worldArea.Width) - r, yWorld]))
                        {
                            aboveWaterRightDist = r;
                            lastTileType = Main.tile[(worldArea.X + worldArea.Width) - r, yWorld].TileType;
                        }
                }

                //bool sidesAboveWaterFilled = (worldArea.X + worldArea.Width) - wallBuffer;
                //!WaterPassThough(Main.tile[(worldArea.X + worldArea.Width) - (wallBuffer - 0), yWorld]) && 
                //!WaterPassThough(Main.tile[worldArea.X + (wallBuffer - 1), yWorld]);

                for (int i = 0; i < worldArea.Width; i++)
                {

                    int xoffset = ((int)worldAreaCorner.X + i);

                    Point position = new Point(worldArea.X + i, yWorld);
                    Tile vanillaTile = Main.tile[position];

                    //center slice
                    AreaArray[xoffset, yoffset, (int)worldAreaCorner.Z] = new BasicTile()
                    {
                        Active = vanillaTile.HasTile,
                        TileType = vanillaTile.TileType,
                        BlockType = vanillaTile.BlockType,
                        Color = vanillaTile.TileColor,
                        TileFrame = new Vector2(vanillaTile.TileFrameX, vanillaTile.TileFrameY),
                        Collide = (Main.tileSolid[vanillaTile.TileType] || !Main.tileSolidTop[vanillaTile.TileType])
                    };
                    AreaArray[xoffset, yoffset, (int)worldAreaCorner.Z].GetTileModel();

                    float featureScale = 5f;//smaller = larger scale
                    float widthMultiplier = 0.93f;//how much of an effect distance from edges has, lower makes the terrain wider
                    float distanceOffset = -0.5f;//a offset to the distance, moves the terrain closer to the center, lower = closer
                    float aboveWallDistanceOffset = 1.2f;
                    bool validValue = TilePositonDistance.TryGetValue(position, out float distance);
                    int evenOffset = (AreaSizeZ % 2 == 0) ? 1 : 0;

                    bool belowWaterHeight = j < worldArea.Height - wallBuffer;
                    bool aboveWaterWall = (aboveWaterLeftDist > 0 || aboveWaterRightDist > 0);
                    if (belowWaterHeight || aboveWaterWall) {
                        int distL = (aboveWaterLeftDist > 0 && aboveWaterRightDist > 0) ? i - aboveWaterLeftDist : (int.MaxValue / 2);
                        int distR = (aboveWaterLeftDist > 0 && aboveWaterRightDist > 0) ? (worldArea.Width - i) - aboveWaterRightDist : (int.MaxValue / 2);
                        int combinedDist = Math.Max(0, Math.Min(distL, distR));

                        for (int k = 0; k < (AreaSizeZ / 2) - evenOffset; k++)
                        {
                            int zoffset = (int)worldAreaCorner.Z + k + 1;
                            float noiseVal = Math.Abs(noise.GetCubicFractal(xoffset * featureScale, yoffset * (featureScale * (!belowWaterHeight ? 0.5f : 2)), zoffset * (featureScale * 2)) * 20);

                            bool inDistance =
                                ((float)(k * widthMultiplier) - noiseVal) > (aboveWaterWall ?
                                combinedDist + aboveWallDistanceOffset :
                                distance + distanceOffset);

                            if (vanillaTile.HasTile && !WaterPassThough(vanillaTile))
                                lastTileType = vanillaTile.TileType;

                            AreaArray[xoffset, yoffset, zoffset] = new BasicTile()
                            {
                                Active =
                                ((belowWaterHeight || (aboveWaterLeftDist > 0 && aboveWaterRightDist > 0)) && zoffset == (AreaSizeZ - 1)) ||
                                (belowWaterHeight && ((!validValue || distance == 0))) || //invalid tiles, or valid ones that are part of ground (originally was just 'belowWaterHeight && !validValue' but this caused some issues withit being wider than the center array
                                inDistance ||
                                (!belowWaterHeight && !WaterPassThough(vanillaTile) && vanillaTile.HasTile && aboveWaterWall),
                                TileType = lastTileType,
                                BlockType = vanillaTile.BlockType,
                                Color = vanillaTile.TileColor,
                                TileFrame = new Vector2(vanillaTile.TileFrameX, vanillaTile.TileFrameY)
                                //collide is left out as the sides should never be a passable tile
                            };
                            AreaArray[xoffset, yoffset, zoffset].GetTileModel();
                        }

                        for (int k = 1; k < (AreaSizeZ / 2f) + evenOffset; k++)
                        {
                            int zoffset = (int)worldAreaCorner.Z - k;
                            float noiseVal = Math.Abs(noise.GetCubicFractal(xoffset * featureScale, yoffset * (featureScale * (!belowWaterHeight ? 0.5f : 2)), zoffset * (featureScale * 2)) * 20);

                            bool inDistance = 
                                ((float)(k * widthMultiplier) - noiseVal) > (aboveWaterWall ? 
                                combinedDist + aboveWallDistanceOffset : 
                                distance + distanceOffset);

                            if (vanillaTile.HasTile && !WaterPassThough(vanillaTile))
                                lastTileType = vanillaTile.TileType;

                            AreaArray[xoffset, yoffset, zoffset] = new BasicTile()
                            {
                                Active = 
                                ((belowWaterHeight || (aboveWaterLeftDist > 0 && aboveWaterRightDist > 0)) && (zoffset == 0)) ||
                                (belowWaterHeight && (!validValue || distance == 0)) || //invalid tiles, or valid ones that are part of ground (originally was just 'belowWaterHeight && !validValue' but this caused some issues withit being wider than the center array
                                inDistance || 
                                (!belowWaterHeight && !WaterPassThough(vanillaTile) && vanillaTile.HasTile && aboveWaterWall), 
                                TileType = lastTileType,
                                BlockType = vanillaTile.BlockType,
                                Color = vanillaTile.TileColor,
                                TileFrame = new Vector2(vanillaTile.TileFrameX, vanillaTile.TileFrameY)
                                //collide is left out as the sides should never be a passable tile
                            };
                            AreaArray[xoffset, yoffset, zoffset].GetTileModel();
                        }
                    }
                }
            }

            //for (int i = 0; i < AreaSizeX; i++)
            //{
            //    for (int j = 0; j < AreaSizeY; j++)
            //    {
            //        for (int k = 0; k < AreaSizeZ; k++)
            //        {
            //            int scale = 3;
            //            float noiseVal = noise.GetCubicFractal(i * scale, j * scale, k * scale) * 25;
            //            AreaArray[i, j, k] = new BasicTile()
            //            {
            //                Active = noiseVal > 0.5f,
            //                TileType = noiseVal < 0.75f ? TileID.Obsidian : TileID.MythrilBrick,
            //            };
            //        }
            //    }
            //}

            //AreaArray[(int)areaCenter.X, (int)areaCenter.Y, (int)areaCenter.Z] = new BasicTile()
            //{
            //    Active = true,
            //    TileType = TileID.LunarOre
            //};

            //AreaArray[AreaSizeX - 1, AreaSizeY - 1, AreaSizeZ - 1] = new BasicTile()
            //{
            //    Active = true,
            //    TileType = TileID.SolarBrick
            //};

            //AreaArray[0, 0, 0] = new BasicTile()
            //{
            //    Active = true,
            //    TileType = TileID.StardustBrick
            //};
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

            CaptureWorldArea(worldrect.Value);//temp name

            WorldGenerated = true;
            return true;
        }
    }
}
