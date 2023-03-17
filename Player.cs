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

        public float BaseMoveSpeed;
        private float CurrentMoveSpeed;

        private World world;
        private Rendering rendering;
        private FishingUIWindow fishingUIWindow;

        public BoundingSphere BoundingSphere;

        public bool ShouldUpdate => fishingUIWindow.WindowActive;
        public bool DebugMode => fishingUIWindow.DebugMode;

        public override void PostAddRecipes()
        {
            world = GetInstance<World>();
            rendering = GetInstance< Rendering> ();
            fishingUIWindow = GetInstance<FishingUIWindow>();
        }

        public void Reset()
        {
            BoundingSphere = new BoundingSphere();
            Position = new Vector3(world.AreaSizeX, world.AreaSizeY, world.AreaSizeZ) * 4;
            Velocity = Vector3.Zero;
            Yaw = 0;
            Pitch = 0;
            BaseMoveSpeed = 0.033f;
            CurrentMoveSpeed = BaseMoveSpeed;
        }

        private Vector3 nextMoveDirection = Vector3.Zero;

        public void UpdateInput(int lastMouseX, int lastMouseY)
        {
            //if (ShouldUpdate)//already only gets called if this is true
            //{
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

                if (DebugMode)
                    CurrentMoveSpeed = 1;

                if (Main.keyState.IsKeyDown(Keys.LeftShift))//may be removed layer or given a sprint bar
                    CurrentMoveSpeed *= 3;

                if (Main.keyState.IsKeyDown(Keys.S))
                    nextMoveDirection.Z += 1;
                if (Main.keyState.IsKeyDown(Keys.W))
                    nextMoveDirection.Z -= 1;
                if (Main.keyState.IsKeyDown(Keys.A))
                    nextMoveDirection.X -= 1;
                if (Main.keyState.IsKeyDown(Keys.D))
                    nextMoveDirection.X += 1;

                //Main.NewText(Pitch);
            //}
        }

        public override void PreUpdatePlayers()
        {
            if (ShouldUpdate)
            {
                if(nextMoveDirection != Vector3.Zero)
                    Velocity += Vector3.Transform(Vector3.Normalize(nextMoveDirection) * CurrentMoveSpeed, Matrix.CreateFromYawPitchRoll(Yaw, Pitch, 0));

                const float SinkSpeed = 0.001f;
                const float SlowDown = 0.942f;

                //gravity
                if (!DebugMode && Velocity.Y < 0.01f)
                    Velocity.Y -= SinkSpeed;

                Position += Velocity;

                int debugBoxsize = 4;
                //sets bounding box to position
                BoundingSphere = new BoundingSphere(Position, debugBoxsize);

                Velocity *= DebugMode ? 0 : SlowDown;

                TileCollisions();

                nextMoveDirection = Vector3.Zero;
                CurrentMoveSpeed = BaseMoveSpeed;
            }
        }

        public void TileCollisions()
        {
            for (int i = -1; i < 4; i++)
            {
                int tilePosX = (int)((BoundingSphere.Center.X / 10f) - 0.5f) + i;
                for (int j = -1; j < 4; j++)
                {
                    int tilePosY = (int)((BoundingSphere.Center.Y / 10f) - 0.5f) + j;
                    for (int k = -1; k < 4; k++)
                    {
                        int tilePosZ = (int)((BoundingSphere.Center.Z / 10f) - 0.5f) + k;

                        if (world.ValidTilePos(tilePosX, tilePosY, tilePosZ) && world.TempCollisionType(tilePosX, tilePosY, tilePosZ) == 1)
                        {
                            BoundingBox tileBox = new BoundingBox(
                                new Vector3(
                                    (tilePosX - 0.5f),
                                    (tilePosY - 0.5f),
                                    (tilePosZ - 0.5f)) * 10,
                                new Vector3(
                                    (tilePosX + 0.5f),
                                    (tilePosY + 0.5f),
                                    (tilePosZ + 0.5f)) * 10);

                            if (BoundingSphere.Intersects(tileBox))
                            {
                                Vector3 dirVector = Vector3.Normalize(BoundingSphere.Center - (new Vector3(tilePosX, tilePosY, tilePosZ) * 10));// * 1.5f;
                                Vector3 dirAbsVector = new Vector3(Math.Abs(dirVector.X), Math.Abs(dirVector.Y), Math.Abs(dirVector.Z));
                                Vector3 roundedDirVec = //new Vector3(Math.Abs((int)dirVector.X), Math.Abs((int)dirVector.Y), Math.Abs((int)dirVector.Z));
                                (dirAbsVector.X > dirAbsVector.Y && dirAbsVector.X > dirAbsVector.Z) ? new Vector3(1, 0, 0) :
                                (dirAbsVector.Y > dirAbsVector.X && dirAbsVector.Y > dirAbsVector.Z) ? new Vector3(0, 1, 0) :
                                (dirAbsVector.Z > dirAbsVector.Y && dirAbsVector.Z > dirAbsVector.X) ? new Vector3(0, 0, 1) :
                                Vector3.One;

                                Main.NewText("|rounded|" + roundedDirVec);

                                Vector3 offset = (BoundingSphere.Center - (new Vector3(tilePosX, tilePosY, tilePosZ) * 10));
                                Vector3 roundedOffset = new Vector3(MathF.Round(offset.X), MathF.Round(offset.Y), MathF.Round(offset.Z));

                                Vector3 output = roundedDirVec * (offset - roundedOffset);
                                Main.NewText("|out|" + output);

                                Position -= output;
                                Velocity = Velocity - (output * 1f);
                            }

                            //Matrix ScalePosBounds = Matrix.CreateScale(1.001f) * Matrix.CreateTranslation(
                            //        new Vector3(
                            //        tilePosX * 10,
                            //        tilePosY * 10,
                            //        tilePosZ * 10));
                            //rendering.DebugCube.Draw(rendering.WorldMatrix * ScalePosBounds, rendering.ViewMatrix, rendering.ProjectionMatrix);
                        }
                    }
                }
            }
        }

        public void DrawPlayer()
        {
            for (int i = -1; i < 4; i++)
            {
                for (int j = -1; j < 4; j++)
                {
                    for (int k = -1; k < 4; k++)
                    {
                        int tilePosX = (int)((BoundingSphere.Center.X / 10f) - 0.5f) + i;
                        int tilePosY = (int)((BoundingSphere.Center.Y / 10f) - 0.5f) + j;
                        int tilePosZ = (int)((BoundingSphere.Center.Z / 10f) - 0.5f) + k;

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