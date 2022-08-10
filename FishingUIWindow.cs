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
        public bool WindowActive = true;

        private Rendering rendering;
        public override void Load()
        {
            rendering = GetInstance<Rendering>();
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            Main.mouseText = true;
            Main.signHover = -1;
            Main.player[Main.myPlayer].mouseInterface = true;
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(rendering.WindowTarget, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);
        }

        public override void PostUpdateInput()
        {
            UpdateInput();  
        }

        private int lastMouseX = 0;
        private int lastMouseY = 0;
        public void UpdateInput()
        {
            if (WindowActive)
                Main.cursorScale = 0;

            if(Main.keyState.IsKeyDown(Keys.NumPad0))
            {
                Main.NewText("Rebuilt vertex buffer");
                rendering.BuildVertexBuffer();
            }

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
}
