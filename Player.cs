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
    internal class RobotPlayer : ModSystem
    {
        public Vector3 Velocity = Vector3.Zero;//unused
        public Vector3 Position = Vector3.Zero;
        public float Yaw = 0;
        public float Pitch = 0;

        private float movementSpeed = 1;

        private World world;
        private Rendering rendering;
        private FishingUIWindow fishingUIWindow;

        public override void PostAddRecipes()
        {
            world = GetInstance<World>();
            rendering = GetInstance< Rendering> ();
            fishingUIWindow = GetInstance<FishingUIWindow>();
        }

        public void Reset()
        {
            Position = new Vector3(world.AreaSizeX, world.AreaSizeY, world.AreaSizeZ) * 4;
            Velocity = Vector3.Zero;
            Yaw = 0;
            Pitch = 0;
            movementSpeed = 0.033f;
        }

        public void UpdateInput(int lastMouseX, int lastMouseY)
        {
            if (fishingUIWindow.WindowActive)
            {
                Yaw -= (Main.mouseX - lastMouseX) * 0.001f;
                Pitch -= (Main.mouseY - lastMouseY) * 0.003f;
                Pitch = Math.Clamp(Pitch, -((float)Math.PI / 2) + 0.05f, (float)Math.PI / 2 - 0.05f);

                if (Main.keyState.IsKeyDown(Keys.Down))
                    Pitch -= 0.015f;
                if (Main.keyState.IsKeyDown(Keys.Up))
                    Pitch += 0.015f;
                if (Main.keyState.IsKeyDown(Keys.Left))
                    Yaw += 0.015f;
                if (Main.keyState.IsKeyDown(Keys.Right))
                    Yaw -= 0.015f;

                Vector3 newDir = Vector3.Zero;
                float moveAmount = fishingUIWindow.DebugMode ? 1 : movementSpeed; 

                if(Main.keyState.IsKeyDown(Keys.LeftShift))//may be removed layer or given a sprint bar
                    moveAmount *= 3;

                if (Main.keyState.IsKeyDown(Keys.S))
                    newDir.Z += moveAmount;
                if (Main.keyState.IsKeyDown(Keys.W))
                    newDir.Z -= moveAmount;
                if (Main.keyState.IsKeyDown(Keys.A))
                    newDir.X -= moveAmount;
                if (Main.keyState.IsKeyDown(Keys.D))
                    newDir.X += moveAmount;

                Velocity += Vector3.Transform(newDir, Matrix.CreateFromYawPitchRoll(Yaw, Pitch, 0));

                //gravity
                if(!fishingUIWindow.DebugMode && Velocity.Y < 0.01f)
                    Velocity.Y -= 0.001f;

                Position += Velocity;
                Velocity *= fishingUIWindow.DebugMode? 0 : 0.942f;

                //Main.NewText(Pitch);
            }
        }
    }
}