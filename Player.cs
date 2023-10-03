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
using System.Numerics;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using static SuperUltraFishing.RobotPlayer;
using static SuperUltraFishing.Collision;
using SuperUltraFishing.Render;
using SuperUltraFishing.World;

namespace SuperUltraFishing
{
    public class RobotPlayer : ModSystem
    {
        public Vector3 Velocity = Vector3.Zero;
        public Vector3 Position = Vector3.Zero;
        public Vector3 OldPosition = Vector3.Zero;
        public float Yaw = 0;
        public float Pitch = 0;

        public float BaseMoveSpeed;
        private float CurrentMoveSpeed;

        private GameWorld world;
        private Rendering rendering;
        private FishingUIWindow fishingUIWindow;

        public BoundingSphere BoundingSphere;
        //public BoundingBox BoundingBox;

        public Vector3 debugVector3 = new Vector3(1, 0, 0);

        public bool ShouldUpdate => fishingUIWindow.WindowActive;
        public bool DebugMode => fishingUIWindow.DebugMode;
        public bool NoClip => fishingUIWindow.NoClip;

        public override void PostAddRecipes()
        {
            world = GetInstance<GameWorld>();
            rendering = GetInstance<Rendering> ();
            fishingUIWindow = GetInstance<FishingUIWindow>();
        }

        public void Reset()
        {
            int debugBoxsize = 4;
            BoundingSphere = new BoundingSphere(Vector3.Zero, debugBoxsize);
            //BoundingBox = new BoundingBox();
            Position = new Vector3(world.GetAreaSizeX, world.GetAreaSizeY, world.GetAreaSizeZ) * 4;//world coords are 1/8th player coords
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

                //keeps player below water height
                float waterlevel = world.WaterLevel * 10;
                const int waterBoundsYOffset = 3;
                if (!DebugMode && Position.Y + waterBoundsYOffset >= waterlevel)
                {
                    if (Position.Y + (waterBoundsYOffset - 1) >= waterlevel)
                        Position.Y -= ((Position.Y + (waterBoundsYOffset - 1.1f)) - waterlevel);

                    Velocity.Y -= 0.075f;
                }

                Position += Velocity;

                BoundingSphere.Center = Position;

                if(!NoClip)
                    TileCollisions();



                Velocity *= DebugMode ? 0 : SlowDown;



                OldPosition = Position;

                nextMoveDirection = Vector3.Zero;
                CurrentMoveSpeed = BaseMoveSpeed;
                //Main.NewText(Position);
            }
        }

        public void TileCollisions()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                    {
                        Position = BoundingSphere.Center = Collision.CollideSphereWithTile(BoundingSphere, i, j, k, world, out bool Collided);
                        //if (Collided)
                        //    Velocity *= 0.9f;
                    }
        }
        public Vector3 TotalDir = Vector3.Zero;
        public List<Vector3> TotalDirList = new List<Vector3>();

        public void DrawPlayer()
        {
            //for (int i = 0; i < 3; i++)
            //    for (int j = 0; j < 3; j++)
            //        for (int k = 0; k < 3; k++)
            //            DrawDebugTileBox(i, j, k);

            //DrawDebugTileBox(0, 1, 1);
            //DrawDebugTileBox(2, 1, 1);
            //DrawDebugTileBox(1, 0, 1);
            //DrawDebugTileBox(1, 2, 1);
            //DrawDebugTileBox(1, 1, 0);
            //DrawDebugTileBox(1, 1, 2);

            if (debugVector3 != Vector3.Zero)
            {
                Matrix ScalePosBounds = Matrix.CreateScale(0.1f) * Matrix.CreateTranslation(debugVector3);
                rendering.Mesh.DebugCube.Draw(rendering.Mesh.WorldMatrix * ScalePosBounds, rendering.Mesh.ViewMatrix, rendering.Mesh.ProjectionMatrix);
            }
        }

        public void DrawDebugTileBox(int i, int j, int k)
        {
            int tilePosX = (int)((Position.X / 10f) - 0.5f) + i;
            int tilePosY = (int)((Position.Y / 10f) - 0.5f) + j;
            int tilePosZ = (int)((Position.Z / 10f) - 0.5f) + k;

            if (world.ValidTilePos(tilePosX, tilePosY, tilePosZ) && world.TempCollisionType(tilePosX, tilePosY, tilePosZ) == 1)
            {
                Matrix ScalePosBounds = Matrix.CreateScale(1.001f) * Matrix.CreateTranslation(
                        new Vector3(
                        tilePosX * 10,
                        tilePosY * 10,
                        tilePosZ * 10));
                rendering.Mesh.DebugCube.Draw(rendering.Mesh.WorldMatrix * ScalePosBounds, rendering.Mesh.ViewMatrix, rendering.Mesh.ProjectionMatrix);
            }
        }
    }
}