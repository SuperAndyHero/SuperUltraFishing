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

        public void BuildLighting()
        {
            (int, int, int)[]

            int sizeX = world.GetAreaSizeX;
            int sizeY = world.GetAreaSizeY;
            int sizeZ = world.GetAreaSizeZ;
            world.LightingArray = new Color[sizeX, sizeY, sizeZ];

            Queue<Color> lightingQueue = new Queue<Color>();
            Color sunColor = new Color(255, 255, 255, 0);//alpha must be zero as its used to track if a light block is sunlight, only propgates downward

            //inital world scan
            for (int i = 0; i < sizeX; i++)//x
            {
                for (int k = 0; k < sizeZ; k++)//z
                {
                    for (int j = 0; j < sizeY; j++)//y
                    {
                        int tiletype = world.TileArray[i, j, k].TileType;

                        if (Main.tileLighted[tiletype])
                        {
                            int ltile = Terraria.Map.MapHelper.tileLookup[tiletype];
                            if (ltile >= GameWorld.ColorLookup.Length)//modded tiles(?)
                                ltile = tiletype;

                            lightingQueue.Enqueue(GameWorld.ColorLookup[ltile]);//alpha should be 255
                        }

                        if (j == 0 && (!world.TileArray[i, j, k].Active || PassLight(world.TileArray[i, j, k].Model)))
                        {
                            lightingQueue.Enqueue(sunColor);
                        }
                    }
                }
            }

            while (lightingQueue.Count > 0)
            {
                //https://www.youtube.com/watch?v=edaaFUflusk
            }
        }
    }
}
