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

namespace SuperUltraFishing
{
    internal class FishingUIWindow : ModSystem
    {
        public bool WindowActive = false;

        private World world;
        private Rendering rendering;

        public Point16 selectedPointA = Point16.Zero;
        public Point16 selectedPointB = Point16.Zero;

        public override void PostAddRecipes()
        {
            world = GetInstance<World>();
            rendering = GetInstance<Rendering>();
        }

        //draw UI and render target to screen
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (WindowActive)
            {
                spriteBatch.Draw(rendering.WindowTarget, new Rectangle(100, 100, Main.screenWidth - 200, Main.screenHeight - 200), Color.White);
            }

            //draw rest of ui here
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
            if (Main.keyState.IsKeyDown(Keys.NumPad8) && !Main.oldKeyState.IsKeyDown(Keys.NumPad8))
            {
                world.GenerateWorld();
                world.CaptureWorldArea(selectedPointA, selectedPointB);
                rendering.BuildVertexBuffer();
                rendering.ResetCamera();
                

                Main.NewText("Starting window");
                WindowActive = true;
            }

            //toggle window active
            if (Main.keyState.IsKeyDown(Keys.NumPad5) && !Main.oldKeyState.IsKeyDown(Keys.NumPad5))
            {
                Main.NewText("Toggled Active");
                WindowActive = !WindowActive;
            }



            if (WindowActive)
            {
                Main.LocalPlayer.frozen = true;
                Main.cursorScale = 0;

                //regen tile array
                if (Main.keyState.IsKeyDown(Keys.NumPad0) && !Main.oldKeyState.IsKeyDown(Keys.NumPad0))
                {
                    Main.NewText("Regenerated Tile Array");
                    world.GenerateWorld();
                    world.CaptureWorldArea(selectedPointA, selectedPointB);
                }

                //build vertex buffer
                if (Main.keyState.IsKeyDown(Keys.NumPad1) && !Main.oldKeyState.IsKeyDown(Keys.NumPad1))
                {
                    Main.NewText("Rebuilt vertex buffer");
                    rendering.BuildVertexBuffer();
                }

                //lock mouse to center
                if ((new Vector2(Main.mouseX, Main.mouseY) - new Vector2(Main.screenWidth / 2, Main.screenHeight / 2)).Length() > 200)
                {
                    int mouseXdiff = Main.mouseX - lastMouseX;
                    int mouseYdiff = Main.mouseY - lastMouseY;
                    Mouse.SetPosition(Main.screenWidth / 2, Main.screenHeight / 2);
                    Main.mouseX = Main.screenWidth / 2;
                    Main.mouseY = Main.screenHeight / 2;
                    lastMouseX = Main.mouseX - mouseXdiff;
                    lastMouseY = Main.mouseY - mouseYdiff;
                }

                rendering.CameraYaw -= (Main.mouseX - lastMouseX) * 0.001f;
                rendering.CameraPitch -= (Main.mouseY - lastMouseY) * 0.003f;

                if (Main.keyState.IsKeyDown(Keys.Down))
                    rendering.CameraPitch -= 0.015f;
                if (Main.keyState.IsKeyDown(Keys.Up))
                    rendering.CameraPitch += 0.015f;
                if (Main.keyState.IsKeyDown(Keys.Left))
                    rendering.CameraYaw += 0.015f;
                if (Main.keyState.IsKeyDown(Keys.Right))
                    rendering.CameraYaw -= 0.015f;

                Vector3 newDir = Vector3.Zero;
                if (Main.keyState.IsKeyDown(Keys.S))
                    newDir.Z += 1;
                if (Main.keyState.IsKeyDown(Keys.W))
                    newDir.Z -= 1;
                if (Main.keyState.IsKeyDown(Keys.A))
                    newDir.X -= 1;
                if (Main.keyState.IsKeyDown(Keys.D))
                    newDir.X += 1;

                rendering.CameraPosition += Vector3.Transform(newDir, Matrix.CreateFromYawPitchRoll(rendering.CameraYaw, rendering.CameraPitch, 0));

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
