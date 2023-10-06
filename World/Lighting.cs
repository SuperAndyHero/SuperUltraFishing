using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using static SuperUltraFishing.BasicTile;
using static Terraria.ModLoader.ModContent;

namespace SuperUltraFishing.World
{
    public class Lighting
    {
        private GameWorld world;

        public Lighting(GameWorld gameworld)
        {
            this.world = gameworld;
        }

        public void PostLoad()
        {

        }

        public static bool PassLight(BlockModelType Model)//this may break with tiles that emit light blocking sun
        {//either add a seperate check like this for sun pass, or remove lighted check from transparent block model in BasicTile
            return Model == BasicTile.BlockModelType.Cross || Model == BasicTile.BlockModelType.CubeTransparent|| Model == BasicTile.BlockModelType.Extruded;
        }

        const int roundFactor = 16;//lower is more precice, but may have issues with non powers of 2
        //0-255, but in (roundFactor) increments
        private int RoundToLightLevel(float colorVal) =>
            (int)MathF.Ceiling((colorVal / (float)roundFactor)) * roundFactor;

        public void BuildLighting()
        {
            int sizeX = world.GetAreaSizeX;
            int sizeY = world.GetAreaSizeY;
            int sizeZ = world.GetAreaSizeZ;
            world.LightingArray = new Color[sizeX, sizeY, sizeZ];

            Queue<(int x, int y, int z)> lightingQueue = new Queue<(int x, int y, int z)>();

            //TODO: change this based on biome and time
            Color sunColor = new Color(255, 255, 255, 255);//alpha must be 255 as its used to track if a light block is sunlight, only propgates downward

            //initial world scan
            for (int i = 0; i < sizeX; i++)//x
            {
                for (int k = 0; k < sizeZ; k++)//z
                {
                    for (int j = 0; j < sizeY; j++)//y
                    {
                        int tiletype = world.TileArray[i, j, k].TileType;

                        bool skylight = false;

                        //TODO: disable this when underground
                        if (j == 0 && (!world.TileArray[i, j, k].Active || PassLight(world.TileArray[i, j, k].Model)) && true)
                        {
                            skylight = true;
                        }

                        if (Main.tileLighted[tiletype])
                        {
                            //solid does not need to be checked for since light tiles can be solid
                            int ltile = Terraria.Map.MapHelper.tileLookup[tiletype];
                            if (ltile >= GameWorld.ColorLookup.Length)//modded tiles(?)
                                ltile = tiletype;

                            Color MapColor = GameWorld.ColorLookup[ltile];

                            int newR = RoundToLightLevel(MapColor.R);
                            int newG = RoundToLightLevel(MapColor.G);
                            int newB = RoundToLightLevel(MapColor.B);

                            world.LightingArray[i, j, k] = new Color(
                               skylight ? Math.Max(newR, sunColor.R) : newR,
                               skylight ? Math.Max(newG, sunColor.G) : newG,
                               skylight ? Math.Max(newB, sunColor.B) : newB, 
                               skylight ? 255 : 0);//alpha is zero for sources, since its used to track sunlight

                            lightingQueue.Enqueue((i, j, k));
                        }
                        else if (skylight)
                        {
                            //alpha is 255, is incremented down by 16 for each water block in the way
                            //this sets the light color which is used for propagation normally
                            world.LightingArray[i, j, k] = sunColor;
                            lightingQueue.Enqueue((i, j, k));
                        }
                    }
                }
            }

            while (lightingQueue.Count > 0)
            {
                (int x, int y, int z) = lightingQueue.Dequeue();
                Color sourceColor = world.LightingArray[x, y, z];
                for (int h = 0; h < 6; h++)
                {
                    //check if light should be updated here here

                    (int offx, int offy, int offz) = SuperUltraFishing.TileSidesOffset(h);
                    int adjacentPosX = x + offx;
                    int adjacentPosY = y + offy;
                    int adjacentPosZ = z + offz;
                    Color adjacentColor = world.LightingArray[adjacentPosX, adjacentPosY, adjacentPosZ];

                    int newR = Math.Max(adjacentColor.R, sourceColor.R - roundFactor);
                    int newG = Math.Max(adjacentColor.G, sourceColor.G - roundFactor);
                    int newB = Math.Max(adjacentColor.B, sourceColor.B - roundFactor);
                    int newA = 0;

                    if (offy == 1 && sourceColor.A > 0)
                    {
                        newA = sourceColor.A - (adjacentPosY < world.WaterLevel ? roundFactor : 0);
                        float alphaMult = (newA / 256f);
                        newR = Math.Max(newR, RoundToLightLevel(sunColor.R * alphaMult));
                        newG = Math.Max(newG, RoundToLightLevel(sunColor.G * alphaMult));
                        newB = Math.Max(newB, RoundToLightLevel(sunColor.B * alphaMult));
                    }

                    if(newR > 0 || newG > 0 || newB > 0 || newA > 0)
                    {
                        world.LightingArray[adjacentPosX, adjacentPosY, adjacentPosZ] = new Color(newR, newG, newB, newA);
                        lightingQueue.Enqueue((adjacentPosX, adjacentPosY, adjacentPosZ));
                    }
                }
            }
        }
    }
}
