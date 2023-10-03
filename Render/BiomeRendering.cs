using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.RuntimeDetour;
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
using ReLogic.Content;
using SuperUltraFishing.World;

namespace SuperUltraFishing.Render
{
    //biome specific data
    public class BiomeRendering
    {
        private GameWorld world;
        private RobotPlayer player;
        private Rendering rendering;

        public Color SkyColor = Color.Purple;

        public BiomeRendering(Rendering rendering)
        {
            this.rendering = rendering;
        }

        public void PostLoad(GameWorld world, RobotPlayer player)
        {
            this.world = world;
            this.player = player;
        }


        private void AddBackgroundRing(Texture2D texture, int planeCount = 16, int loopCount = 4, float distance = 64f, float worldHeight = 10, float rotationOffset = 0f, Vector3 offset = default, Color color = default, int frameCount = 1) =>
            rendering.Mesh.BuildBackgroundRing(texture, planeCount, loopCount, distance, worldHeight, rotationOffset, offset, color, frameCount);

        //called during mesh generation
        //could be extracted out into a biome data class which is not related to rendering, which would obsolete this class
        public void SetBackground()
        {
            SkyColor = new Color(90, 170, 230, 255);

            var a = Main.treeBGSet2[0];
            var b = Main.snowBG;
            var c = Main.snowMntBG;
            switch (Main.bgStyle)
            {
                case SurfaceBackgroundID.Desert:
                    {
                        //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.desertBG[2], AssetRequestMode.ImmediateLoad).Value, worldHeight: -20, loopCount: 4, distance: 58f);
                        //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.desertBG[1], AssetRequestMode.ImmediateLoad).Value, worldHeight: 32, loopCount: 2, distance: 64f, rotationOffset: (float)Math.Tau * 0.66f);
                        //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.desertBG[0], AssetRequestMode.ImmediateLoad).Value, worldHeight: 42, loopCount: 2, distance: 76f, rotationOffset: (float)Math.Tau * 0.166f, color: new Color(230, 230, 230, 200));
                        SkyColor = new Color(145, 165, 245, 255);
                    }
                    break;
                case SurfaceBackgroundID.GoodEvilDesert:
                    {
                        switch (WorldGen.desertBG)
                        {
                            case 0:
                                {
                                    AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.desertBG[1], AssetRequestMode.ImmediateLoad).Value, worldHeight: -24, loopCount: 4, distance: 58f, color: new Color(200, 150, 150, 255));
                                    AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.desertBG[0], AssetRequestMode.ImmediateLoad).Value, worldHeight: -10, loopCount: 6, distance: 64f, rotationOffset: (float)Math.Tau * 0.66f, color: new Color(200, 150, 150, 255));
                                    AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + 22, AssetRequestMode.ImmediateLoad).Value, worldHeight: -30, loopCount: 2, distance: 76f, rotationOffset: (float)Math.Tau * 0.166f, color: new Color(200, 150, 150, 255));
                                    AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + 23, AssetRequestMode.ImmediateLoad).Value, worldHeight: -10, loopCount: 4, distance: 96f, rotationOffset: (float)Math.Tau * 0.24f, color: new Color(200, 150, 150, 255));
                                    SkyColor = new Color(145, 165, 245, 255);
                                }
                                break;
                        }
                    }
                    break;
                case SurfaceBackgroundID.Forest1:
                    {
                        //TODO: most bgs have a seperate mountain array (also use second switch statement, for a total of 2 within this case)
                        AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.treeBGSet1[0], AssetRequestMode.ImmediateLoad).Value, worldHeight: 20, loopCount: 2, distance: 52f);
                        AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.treeMntBGSet1[1], AssetRequestMode.ImmediateLoad).Value, worldHeight: 28, loopCount: 2, distance: 72f, rotationOffset: (float)Math.Tau * 0.2f);
                        AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.treeMntBGSet1[0], AssetRequestMode.ImmediateLoad).Value, worldHeight: 22, loopCount: 2, distance: 92f, rotationOffset: (float)Math.Tau * 0.2f);
                        SkyColor = new Color(103, 142, 248, 255);
                    }
                    break;
                case SurfaceBackgroundID.Forest2:
                    {
                        //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.treeBGSet2[0], AssetRequestMode.ImmediateLoad).Value, worldHeight: -4, planeCount: 16, loopCount: 2);
                        //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.treeBGSet2[1], AssetRequestMode.ImmediateLoad).Value, distMult: 2.5f, worldHeight: -4, rotationOffset: (float)Math.Tau * 0.66f);
                        //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.treeBGSet2[2], AssetRequestMode.ImmediateLoad).Value, planeCount: 8, distMult: 3.2f, worldHeight: 8, rotationOffset: (float)Math.Tau * 0.166f);
                    }
                    break;
                case SurfaceBackgroundID.Forest3:
                    break;
                case SurfaceBackgroundID.Forest4:
                    break;
                case SurfaceBackgroundID.Snow:
                    {
                        switch (WorldGen.snowBG)
                        {
                            case 0:
                                {
                                    //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.snowBG[2], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 10, loopCount: 4, distance: 64f);
                                    //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.snowBG[1], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 35, loopCount: 4, rotationOffset: MathF.Tau * 0.22f, distance: 100f);
                                    //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.snowBG[0], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 50, rotationOffset: MathF.Tau * 0.166f, distance: 120f);
                                    AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.snowMntBG[1], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 15, loopCount: 4, rotationOffset: MathF.Tau * 0.22f, distance: 100f);
                                    AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.snowMntBG[0], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 30, rotationOffset: MathF.Tau * 0.166f, distance: 120f);
                                    SkyColor = new Color(85, 146, 168, 255);
                                }
                                break;
                            case 2:
                                {//no snowBG for this style
                                    AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.snowMntBG[1], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 15, loopCount: 4, rotationOffset: MathF.Tau * 0.22f, distance: 100f);
                                    AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.snowMntBG[0], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 30, rotationOffset: MathF.Tau * 0.166f, distance: 120f);
                                    SkyColor = new Color(85, 146, 168, 255);
                                }
                                break;
                        }
                        //if(Main.snowBG[2] == 39)
                        //    AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.snowBG[2], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 10, loopCount: 4, distance: 64f);
                        //else
                        //    AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.snowBG[2], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: -60, loopCount: 6, distance: 64f);
                        //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.snowBG[1], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 35, loopCount: 4, rotationOffset: MathF.Tau * 0.22f, distance: 100f);
                        //AddBackgroundRing(ModContent.Request<Texture2D>("Terraria/Images/Background_" + Main.snowBG[0], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 50, rotationOffset: MathF.Tau * 0.166f, distance: 120f);
                    }
                    break;
                case SurfaceBackgroundID.Jungle:
                    {
                        switch (WorldGen.jungleBG)//todo, add secondary switch statements using the world bg value
                        {
                            case 0:
                                AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.jungleBG[2], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: -24, distance: 64f);
                                AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.jungleBG[1], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: -8, rotationOffset: MathF.Tau * 0.22f, distance: 72f);
                                AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.jungleBG[0], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 10, rotationOffset: MathF.Tau * 0.166f, distance: 86f);
                                break;
                            case 5:
                                AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.jungleBG[2], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: -0, loopCount: 4, distance: 64f, frameCount: 2);
                                AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.jungleBG[1], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 3, loopCount: 4, rotationOffset: MathF.Tau * 0.22f, distance: 72f, frameCount: 2);
                                AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.jungleBG[0], AssetRequestMode.ImmediateLoad).Value, planeCount: 16, worldHeight: 9, loopCount: 4, rotationOffset: MathF.Tau * 0.166f, distance: 86f, frameCount: 2);
                                break;
                        }

                        SkyColor = new Color(85, 146, 168, 255);
                    }
                    break;
                case SurfaceBackgroundID.Ocean:
                    {
                        AddBackgroundRing(Request<Texture2D>("Terraria/Images/Background_" + Main.oceanBG, AssetRequestMode.ImmediateLoad).Value, planeCount: 16, loopCount: 6, worldHeight: -4, distance: 86f);
                    }
                    break;
            }
        }
    }
}
