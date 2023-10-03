using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.RuntimeDetour;
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
using SuperUltraFishing.Render;
using static Terraria.ModLoader.ModContent;
using SuperUltraFishing.World;

namespace SuperUltraFishing
{
    internal class FishingUIWindow : ModSystem
    {
        public bool DebugMode = false;
        public bool NoClip = false;
        public bool WindowActive = false;

        private GameWorld world;
        private Rendering rendering;
        private RobotPlayer player;
        private EntitySystem entitySystem;

        public Point16 selectedPointA = Point16.Zero;
        public Point16 selectedPointB = Point16.Zero;

        public override void PostAddRecipes()
        {
            world = GetInstance<GameWorld>();
            rendering = GetInstance<Rendering>();
            player = GetInstance<RobotPlayer>();
            entitySystem = GetInstance<EntitySystem>();
        }

        public void ActivateWindow(Point16 worldLocation)
        {
            bool successful = world.GenerateWorld(worldLocation);

            if(!string.IsNullOrEmpty(world.Error))
                Main.NewText("Error: " + world.Error, Color.IndianRed);

            if (!successful)
                return;

            rendering.Mesh.Build();
            player.Reset();


            Main.NewText("Starting window");
            WindowActive = true;
        }

        //draw UI and render target to screen
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (WindowActive)
            {
                spriteBatch.End();
                Color backgroundColor = rendering.Biome.SkyColor;
                Rectangle windowSize = new Rectangle(100, 100, Main.screenWidth - 200, Main.screenHeight - 200);

                //extra end/begin so that the blank background isnt effect by ui scale
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null);
                spriteBatch.Draw(Terraria.GameContent.TextureAssets.BlackTile.Value, new Rectangle(100, 100, Main.screenWidth - 200, Main.screenHeight - 200), backgroundColor);
                spriteBatch.End();

                //everything drawn in this sb will be effected by the water shader
                rendering.WaterPostProcessEffect.Parameters["DistortMap"].SetValue((Texture)(object)rendering.WaterTarget);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, rendering.WaterPostProcessEffect);
                spriteBatch.Draw(rendering.WindowTarget, windowSize, Color.White);
                spriteBatch.End();

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null);
                rendering.UI.DrawUI(spriteBatch, windowSize);
                spriteBatch.End();

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
                //spriteBatch.Draw(rendering.WaterTarget, new Rectangle((int)(Main.screenWidth / 1.75f), (int)(Main.screenHeight / 1.75f), (int)(Main.screenWidth / 2.75f), (int)(Main.screenHeight / 2.75f)), Color.White);
                //draw rest of ui here
            }
        }

        private int lastMouseX = 0;
        private int lastMouseY = 0;
        public override void PostUpdateInput()
        {
            //point a
            if (Main.keyState.IsKeyDown(Keys.NumPad7) && !Main.oldKeyState.IsKeyDown(Keys.NumPad7))
            {
                Vector2 pos = Main.MouseWorld / 16;
                selectedPointA = pos.ToPoint16();
                for (int i = 0; i < 10; i++)
                    Dust.NewDust(pos * 16, 0, 0, DustID.Torch);

                Main.NewText("Point A set to: " + pos);
            }

            if (Main.keyState.IsKeyDown(Keys.NumPad8) && !Main.oldKeyState.IsKeyDown(Keys.NumPad8))
            {
                Main.NewText("opening window via debug");
                world.DebugGenerateWorld(new Rectangle(selectedPointA.X, selectedPointA.Y, selectedPointB.X - selectedPointA.X, selectedPointB.Y - selectedPointA.Y));
                rendering.Mesh.Build();
                player.Reset();


                Main.NewText("Starting window");
                WindowActive = true;
            }

            //point b
            if (Main.keyState.IsKeyDown(Keys.NumPad9) && !Main.oldKeyState.IsKeyDown(Keys.NumPad9))
            {
                Vector2 pos = Main.MouseWorld / 16;
                selectedPointB = pos.ToPoint16();
                for (int i = 0; i < 10; i++)
                    Dust.NewDust(pos * 16, 0, 0, DustID.BlueTorch);

                Main.NewText("Point B set to: " + pos);
            }

            //start window
            if (Main.keyState.IsKeyDown(Keys.OemPeriod) && !Main.oldKeyState.IsKeyDown(Keys.OemPeriod))
            {
                Main.NewText("Toggled Debug");
                DebugMode = !DebugMode;
            }

            if (Main.keyState.IsKeyDown(Keys.NumPad3) && !Main.oldKeyState.IsKeyDown(Keys.NumPad3))
            {
                Main.NewText("Toggled NoClip");
                NoClip = !NoClip;
            }

            //toggle window active
            if (Main.keyState.IsKeyDown(Keys.NumPad5) && !Main.oldKeyState.IsKeyDown(Keys.NumPad5))
            {
                Main.NewText("Toggled Active");
                WindowActive = !WindowActive;
            }

            //spawn fish
            if (Main.keyState.IsKeyDown(Keys.Insert) && !Main.oldKeyState.IsKeyDown(Keys.Insert))
            {
                if(Main.keyState.IsKeyDown(Keys.LeftShift))
                    entitySystem.SpawnEntity(typeof(FishBone), player.Position);
                else
                    entitySystem.SpawnEntity(typeof(SimpleFish), player.Position);
                Main.NewText("Spawn Fish Bone");
                //entitySystem.SpawnEntity(typeof(FishBone), player.Position);
            }



            if (WindowActive)
            {
                //regen tile array
                if (Main.keyState.IsKeyDown(Keys.NumPad0) && !Main.oldKeyState.IsKeyDown(Keys.NumPad0))
                {
                    Main.NewText("Regenerated Tile Array");
                    world.GenerateWorld(world.LastWorldLocation);
                }

                //build vertex buffer
                if (Main.keyState.IsKeyDown(Keys.NumPad1) && !Main.oldKeyState.IsKeyDown(Keys.NumPad1))
                {
                    Main.NewText("Rebuilt vertex buffer");
                    rendering.Mesh.Build();
                }

                //Close window with escape
                //todo: add confirm message
                if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
                    WindowActive = false;

                Main.LocalPlayer.frozen = true;
                Main.LocalPlayer.noKnockback = true;
                Main.LocalPlayer.immune = true;
                Main.LocalPlayer.immuneTime = 10;
                Main.LocalPlayer.statLife = Main.LocalPlayer.statLifeMax;//temp solution, this needs to freeze health instead to prevent exploits
                Main.cursorScale = 0;

                //lock mouse to center
                if (!Main.keyState.IsKeyDown(Keys.LeftControl) &&(new Vector2(Main.mouseX, Main.mouseY) - new Vector2(Main.screenWidth / 2, Main.screenHeight / 2)).Length() > 200)
                {
                    int mouseXdiff = Main.mouseX - lastMouseX;
                    int mouseYdiff = Main.mouseY - lastMouseY;
                    Mouse.SetPosition(Main.screenWidth / 2, Main.screenHeight / 2);
                    Main.mouseX = Main.screenWidth / 2;
                    Main.mouseY = Main.screenHeight / 2;
                    lastMouseX = Main.mouseX - mouseXdiff;
                    lastMouseY = Main.mouseY - mouseYdiff;
                }

                player.UpdateInput(lastMouseX, lastMouseY);

                lastMouseX = Main.mouseX;
                lastMouseY = Main.mouseY;
            }
        }

        //hides mouse text
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (WindowActive)
            {
                Main.mouseText = true;
                Main.signHover = -1;
                Main.player[Main.myPlayer].mouseInterface = true;
            }
        }
    }
}
