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

namespace SuperUltraFishing.World
{
    public class GameWorld : ModSystem
    {
        public EntitySystem entitySystem;
        public Generation Generation;
        public Spawning Spawning;
        public Lighting Lighting;

        public BasicTile[,,] TileArray = null;
        public Color[,,] LightingArray = null;//unused

        public int WaterLevel;

        public int GetAreaSizeX => TileArray.GetLength(0);
        public int GetAreaSizeY => TileArray.GetLength(1);
        public int GetAreaSizeZ => TileArray.GetLength(2);

        public string Error = "";

        public static HashSet<ushort> FourSidedTiles;//top and bottom are different (may not be needed?)
        //public HashSet<ushort> FourSidedTilesBL;
        public static HashSet<ushort> CrossTile;//'Minecraft grass' style model

        public static Color[] ColorLookup;//color array for blocks

        public override void Load()
        {
            Generation = new Generation(this);
            Spawning = new Spawning(this);
            Lighting = new Lighting(this);

            if (!Main.dedServ)//deside if some stuff should be synced, or if it should mostly all be clientside
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

                FieldInfo fieldInfo = typeof(Terraria.Map.MapHelper).GetField("colorLookup", BindingFlags.NonPublic | BindingFlags.Static);
                ColorLookup = (Color[])fieldInfo.GetValue(null);
            }

            //FourSidedTilesBL = new HashSet<ushort>
            //{
            //    TileID.CorruptThorns,
            //    TileID.CrimsonThorns,
            //    TileID.JungleThorns,
            //    TileID.Cactus
            //};
        }

        public override void PostAddRecipes()
        {
            entitySystem = GetInstance<EntitySystem>();
            Generation.PostLoad(entitySystem);
            Spawning.PostLoad(entitySystem);
            Lighting.PostLoad();
        }

        public override void Unload()//unused
        {
            base.Unload();
        }

        //public void PlaceTile(ushort type, int x, int y, int z)
        //{
        //    TileArray[x, y, z].TileType = type;
        //    TileArray[x, y, z].Active = true;
        //}

        public bool ValidTilePos(int x, int y, int z)
        {
            return
                x >= 0 && x < GetAreaSizeX &&
                y >= 0 && y < GetAreaSizeY &&
                z >= 0 && z < GetAreaSizeZ;
        }

        public int TempCollisionType(int x, int y, int z)
        {
            //temp solution until a proper collision type is implemented
            //for now just returns if a tile is active or now
            return TileArray[x, y, z].Active ? 1 : 0;
        }

        //unused and imcomplete
        //public void RemoveWorld()//unload world
        //{
        //    TileArray = null;
        //    WorldGenerated = false;
        //}

        public bool StartWorld(Point16 worldLocation)
        {
            entitySystem.ClearAllEntities();
            Error = "";

            bool success = Generation.GenerateWorld(worldLocation);

            Lighting.BuildLighting();

            Spawning.SpawnEntities();

            return success;
        }

        //public bool DebugGenerateWorld(Rectangle area)
        //{
        //    entitySystem.ClearAllEntities();
        //    Error = "";
        //    WorldGenerated = false;
        //    CaptureWorldArea(area);//temp name

        //    WorldGenerated = true;
        //    return true;
        //}
    }
}
