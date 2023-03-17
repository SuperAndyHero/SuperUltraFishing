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
using System.Security.Cryptography.X509Certificates;

namespace SuperUltraFishing
{
    internal class RobotPlayer : ModSystem
    {
        public Vector3 Velocity = Vector3.Zero;
        public Vector3 Position = Vector3.Zero;
        public float Yaw = 0;
        public float Pitch = 0;

        private float movementSpeed = 1;

        private World world;
        private Rendering rendering;
        private FishingUIWindow fishingUIWindow;

        public BoundingSphere debugBoundingSphere;

        public override void PostAddRecipes()
        {
            world = GetInstance<World>();
            rendering = GetInstance< Rendering> ();
            fishingUIWindow = GetInstance<FishingUIWindow>();
        }

        public void Reset()
        {
            debugBoundingSphere = new BoundingSphere();
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


                //move to proper update method
                Velocity += Vector3.Transform(newDir, Matrix.CreateFromYawPitchRoll(Yaw, Pitch, 0));

                const float SinkSpeed = 0.001f;
                const float SlowDown = 0.942f;

                //gravity
                if (!fishingUIWindow.DebugMode && Velocity.Y < 0.01f)
                    Velocity.Y -= SinkSpeed;

                Position += Velocity;

                int debugBoxsize = 10;
                //sets bounding box to position
                debugBoundingSphere = new BoundingSphere(Position, debugBoxsize);

                Velocity *= fishingUIWindow.DebugMode? 0 : SlowDown;

                //Main.NewText(Pitch);
            }
        }

        public void DrawPlayer()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int tilePosX = (int)((debugBoundingSphere.Center.X / 10f) - 0.5f) + i;
                        int tilePosY = (int)((debugBoundingSphere.Center.Y / 10f) - 0.5f) + j;
                        int tilePosZ = (int)((debugBoundingSphere.Center.Z / 10f) - 0.5f) + k;

                        if (world.ValidTilePos(tilePosX, tilePosY, tilePosZ) && world.TempCollisionType(tilePosX, tilePosY, tilePosZ) == 1)
                        {
                            Matrix ScalePosBounds = Matrix.CreateScale(1.001f) * Matrix.CreateTranslation(
                                    new Vector3(
                                    tilePosX * 10,
                                    tilePosY * 10,
                                    tilePosZ * 10));
                            rendering.DebugCube.Draw(rendering.WorldMatrix * ScalePosBounds, rendering.ViewMatrix, rendering.ProjectionMatrix);
                        }
                    }
                }
            }
        }
    }
}