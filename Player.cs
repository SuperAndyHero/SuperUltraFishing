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

namespace SuperUltraFishing
{
    internal class RobotPlayer : ModSystem
    {
        public Vector3 Velocity = Vector3.Zero;
        public Vector3 Position = Vector3.Zero;
        public Vector3 OldPosition = Vector3.Zero;
        public float Yaw = 0;
        public float Pitch = 0;

        public float BaseMoveSpeed;
        private float CurrentMoveSpeed;

        private World world;
        private Rendering rendering;
        private FishingUIWindow fishingUIWindow;

        public BoundingSphere BoundingSphere;
        //public BoundingBox BoundingBox;

        public Vector3 debugVector3 = new Vector3(1, 0, 0);

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
            int debugBoxsize = 4;
            BoundingSphere = new BoundingSphere(Vector3.Zero, debugBoxsize);
            //BoundingBox = new BoundingBox();
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

                {
                    Position += Velocity;

                    //sets bounding box to position
                    //Vector3 off = new Vector3(debugBoxsize);
                    BoundingSphere.Center = Position;
                    //BoundingBox = new BoundingBox(Position - off, Position + off);
                    TileCollisions();
                }

                Velocity *= DebugMode ? 0 : SlowDown;

                OldPosition = Position;

                nextMoveDirection = Vector3.Zero;
                CurrentMoveSpeed = BaseMoveSpeed;
            }
        }

        public void TileCollisions()
        {
            //CollideWithTile(1, 1, 1);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                        CollideWithTile(i, j, k);

            //CollideWithTile(0, 1, 1);
            //CollideWithTile(2, 1, 1);
            //CollideWithTile(1, 0, 1);//down
            //CollideWithTile(1, 2, 1);//up
            //CollideWithTile(1, 1, 0);
            //CollideWithTile(1, 1, 2);
            //Vector3 MaxVec = Vector3.Zero;
            //float MaxVecLength = 0;
            //float AvrVecLength = 0;

            //foreach (var vec in TotalDirList)
            //{
            //    TotalDir += vec;
            //    float veclen = vec.Length();
            //    AvrVecLength += veclen / TotalDirList.Count;
            //    if (veclen > MaxVecLength)
            //    {
            //        MaxVecLength = veclen;
            //        //MaxVec = vec;
            //    }
            //}
            //Vector3 norDir = TotalDir == Vector3.Zero ? Vector3.Zero : Vector3.Normalize(TotalDir);

            //Position += norDir * MaxVecLength;

            //TotalDir = Vector3.Zero;
            //TotalDirList.Clear();
        }
        public Vector3 TotalDir = Vector3.Zero;
        public List<Vector3> TotalDirList = new List<Vector3>();
        public void CollideWithTile(int i, int j, int k)
        {
            int tilePosX = (int)((Position.X / 10f) - 0.5f) + i;
            int tilePosY = (int)((Position.Y / 10f) - 0.5f) + j;
            int tilePosZ = (int)((Position.Z / 10f) - 0.5f) + k;

            if (world.ValidTilePos(tilePosX, tilePosY, tilePosZ) && world.TempCollisionType(tilePosX, tilePosY, tilePosZ) == 1)
            {
                //List<(Vector3 off, Vector3 closestPoint)> CollisionList = new List<(Vector3 off, Vector3 closestPoint)>();

                //OPIMIZATION TODO: since this checks which side is closest anyway, use of triangles could be removed completely

                void CheckTriangle(Triangle triangle)
                {
                    //clips into corners because block isnt checked
                    if (SphereIntersectsTriangle(BoundingSphere, triangle, out Vector3 closestPointOnTri))
                    {
                        Position += triangle.Normal *
                            (Vector3.Distance(BoundingSphere.Center, closestPointOnTri) - BoundingSphere.Radius);

                        BoundingSphere.Center = Position;
                        Velocity *= 1f;//friction (help prevents studders when going over corner)
                    }
                }

                //this list is so that the closest side can be moved to the front, this prevents one side taking priority
                const float defaultDist = 100000;
                float lastDistToCenter = defaultDist;
                List<(Triangle triA, Triangle triB)> CollisionFaceList = new List<(Triangle triA, Triangle triB)>();

                //up
                if (world.ValidTilePos(tilePosX, tilePosY + 1, tilePosZ) && world.TempCollisionType(tilePosX, tilePosY + 1, tilePosZ) != 1)
                {
                    float distance = Vector3.Distance(new Vector3(tilePosX, tilePosY + 0.5f, tilePosZ) * 10, Position);
                    int index = 1;
                    if(distance < lastDistToCenter)
                    {
                        lastDistToCenter = distance;
                        index = 0;
                    }
                    CollisionFaceList.Insert(index, (
                    new Triangle(
                            new Vector3(
                                (tilePosX - 0.5f),
                                (tilePosY + 0.5f),
                                (tilePosZ - 0.5f)) * 10,
                            new Vector3(
                                (tilePosX - 0.5f),
                                (tilePosY + 0.5f),
                                (tilePosZ + 0.5f)) * 10,
                            new Vector3(
                                (tilePosX + 0.5f),
                                (tilePosY + 0.5f),
                                (tilePosZ - 0.5f)) * 10),
                    new Triangle(
                            new Vector3(
                                (tilePosX + 0.5f),
                                (tilePosY + 0.5f),
                                (tilePosZ + 0.5f)) * 10,
                            new Vector3(
                                (tilePosX + 0.5f),
                                (tilePosY + 0.5f),
                                (tilePosZ - 0.5f)) * 10,
                            new Vector3(
                                (tilePosX - 0.5f),
                                (tilePosY + 0.5f),
                                (tilePosZ + 0.5f)) * 10)
                    ));
                }

                //down
                if (world.ValidTilePos(tilePosX, tilePosY - 1, tilePosZ) && world.TempCollisionType(tilePosX, tilePosY - 1, tilePosZ) != 1)
                {
                    float distance = Vector3.Distance(new Vector3(tilePosX, tilePosY - 0.5f, tilePosZ) * 10, Position);
                    int index = 1;
                    if (distance < lastDistToCenter)
                    {
                        lastDistToCenter = distance;
                        index = 0;
                    }
                    CollisionFaceList.Insert(index, (
                    new Triangle(
                         new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ - 0.5f)) * 10,
                        new Vector3(
                            (tilePosX + 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ - 0.5f)) * 10,
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ + 0.5f)) * 10),
                    new Triangle(
                            new Vector3(
                                (tilePosX + 0.5f),
                                (tilePosY - 0.5f),
                                (tilePosZ + 0.5f)) * 10,
                            new Vector3(
                                (tilePosX - 0.5f),
                                (tilePosY - 0.5f),
                                (tilePosZ + 0.5f)) * 10,
                            new Vector3(
                                (tilePosX + 0.5f),
                                (tilePosY - 0.5f),
                                (tilePosZ - 0.5f)) * 10)
                    ));
                }

                //right
                if (world.ValidTilePos(tilePosX + 1, tilePosY, tilePosZ) && world.TempCollisionType(tilePosX + 1, tilePosY, tilePosZ) != 1)
                {
                    float distance = Vector3.Distance(new Vector3(tilePosX + 0.5f, tilePosY, tilePosZ) * 10, Position);
                    int index = 1;
                    if (distance < lastDistToCenter)
                    {
                        lastDistToCenter = distance;
                        index = 0;
                    }
                    CollisionFaceList.Insert(index, (
                    new Triangle(
                         new Vector3(
                            (tilePosX + 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ - 0.5f)) * 10,
                        new Vector3(
                            (tilePosX + 0.5f),
                            (tilePosY + 0.5f),
                            (tilePosZ - 0.5f)) * 10,
                        new Vector3(
                            (tilePosX + 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ + 0.5f)) * 10),
                    new Triangle(
                            new Vector3(
                                (tilePosX + 0.5f),
                                (tilePosY + 0.5f),
                                (tilePosZ + 0.5f)) * 10,
                            new Vector3(
                                (tilePosX + 0.5f),
                                (tilePosY - 0.5f),
                                (tilePosZ + 0.5f)) * 10,
                            new Vector3(
                                (tilePosX + 0.5f),
                                (tilePosY + 0.5f),
                                (tilePosZ - 0.5f)) * 10)
                    ));
                }

                //left
                if (world.ValidTilePos(tilePosX - 1, tilePosY, tilePosZ) && world.TempCollisionType(tilePosX - 1, tilePosY, tilePosZ) != 1)
                {
                    float distance = Vector3.Distance(new Vector3(tilePosX - 0.5f, tilePosY, tilePosZ) * 10, Position);
                    int index = 1;
                    if (distance < lastDistToCenter)
                    {
                        lastDistToCenter = distance;
                        index = 0;
                    }
                    CollisionFaceList.Insert(index, (
                    new Triangle(
                         new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ - 0.5f)) * 10,
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ + 0.5f)) * 10,
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY + 0.5f),
                            (tilePosZ - 0.5f)) * 10),
                    new Triangle(
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY + 0.5f),
                            (tilePosZ + 0.5f)) * 10,
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY + 0.5f),
                            (tilePosZ - 0.5f)) * 10,
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ + 0.5f)) * 10)
                    ));
                }

                //front
                if (world.ValidTilePos(tilePosX, tilePosY, tilePosZ + 1) && world.TempCollisionType(tilePosX, tilePosY, tilePosZ + 1) != 1)
                {
                    float distance = Vector3.Distance(new Vector3(tilePosX, tilePosY, tilePosZ + 0.5f) * 10, Position);
                    int index = 1;
                    if (distance < lastDistToCenter)
                    {
                        lastDistToCenter = distance;
                        index = 0;
                    }
                    CollisionFaceList.Insert(index, (
                    new Triangle(
                         new Vector3(
                            (tilePosX + 0.5f),
                            (tilePosY + 0.5f),
                            (tilePosZ + 0.5f)) * 10,
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY + 0.5f),
                            (tilePosZ + 0.5f)) * 10,
                        new Vector3(
                            (tilePosX + 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ + 0.5f)) * 10),
                    new Triangle(
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ + 0.5f)) * 10,
                        new Vector3(
                            (tilePosX + 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ + 0.5f)) * 10,
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY + 0.5f),
                            (tilePosZ + 0.5f)) * 10)
                    ));
                }

                //back
                if (world.ValidTilePos(tilePosX, tilePosY, tilePosZ - 1) && world.TempCollisionType(tilePosX, tilePosY, tilePosZ - 1) != 1)
                {
                    float distance = Vector3.Distance(new Vector3(tilePosX, tilePosY, tilePosZ - 0.5f) * 10, Position);
                    int index = 1;
                    if (distance < lastDistToCenter)
                    {
                        lastDistToCenter = distance;
                        index = 0;
                    }
                    CollisionFaceList.Insert(index, (
                    new Triangle(
                         new Vector3(
                            (tilePosX + 0.5f),
                            (tilePosY + 0.5f),
                            (tilePosZ - 0.5f)) * 10,
                        new Vector3(
                            (tilePosX + 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ - 0.5f)) * 10,
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY + 0.5f),
                            (tilePosZ - 0.5f)) * 10),
                    new Triangle(
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ - 0.5f)) * 10,
                        new Vector3(
                            (tilePosX - 0.5f),
                            (tilePosY + 0.5f),
                            (tilePosZ - 0.5f)) * 10,
                        new Vector3(
                            (tilePosX + 0.5f),
                            (tilePosY - 0.5f),
                            (tilePosZ - 0.5f)) * 10)
                    ));
                }

                //bool done = false;
                foreach (var obj in CollisionFaceList)
                {
                    //if (!done)
                    //{
                    //    debugVector3 = (obj.triA.PointA + obj.triA.PointB + obj.triA.PointC) / 3;
                    //    done = true;
                    //}
                    CheckTriangle(obj.triA);
                    CheckTriangle(obj.triB);
                }
            }
        }

        public struct Triangle
        {
            public Vector3 PointA;
            public Vector3 PointB;
            public Vector3 PointC;

            public Vector3 Normal;

            public Triangle(Vector3 PointA, Vector3 PointB, Vector3 PointC)
            {
                this.PointA = PointA; 
                this.PointB = PointB;
                this.PointC = PointC;
                Normal = Rendering.CalculateNormal(PointA, PointB, PointC);
            }
        }
        public bool SphereIntersectsTriangle(BoundingSphere sphere, Triangle triangle, out Vector3 point) =>
            SphereIntersectsTriangle(sphere, triangle.PointA, triangle.PointB, triangle.PointC, out point);
        public bool SphereIntersectsTriangle(BoundingSphere sphere, Vector3 a, Vector3 b, Vector3 c, out Vector3 point)
        {
            // Find point P on triangle ABC closest to sphere center
            point = ClosestPointOnTriangle(sphere.Center, a, b, c);
            // Sphere and triangle intersect if the (squared) distance from sphere
            // center to point p is less than the (squared) sphere radius
            Vector3 v = point - sphere.Center;
            return Vector3.Dot(v, v) <= sphere.Radius * sphere.Radius;
        }
        public Vector3 ClosestPointOnTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            // Check if P in vertex region outside A
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = point - a;
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f) return a; // barycentric coordinates (1,0,0)
                                                    // Check if P in vertex region outside B
            Vector3 bp = point - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3) return b; // barycentric coordinates (0,1,0)
                                                  // Check if P in edge region of AB, if so return projection of P onto AB
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                float vd = d1 / (d1 - d3);
                return a + vd * ab; // barycentric coordinates (1-v,v,0)
            }
            // Check if P in vertex region outside C
            Vector3 cp = point - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6) return c; // barycentric coordinates (0,0,1)

            // Check if P in edge region of AC, if so return projection of P onto AC
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                float wd = d2 / (d2 - d6);
                return a + wd * ac; // barycentric coordinates (1-w,0,w)
            }
            // Check if P in edge region of BC, if so return projection of P onto BC
            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                float wf = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + wf * (c - b); // barycentric coordinates (0,1-w,w)
            }
            // P inside face region. Compute Q through its barycentric coordinates (u,v,w)
            float denom = 1.0f / (va + vb + vc);
            float v = vb * denom;
            float w = vc * denom;
            return a + ab * v + ac * w; // = u*a + v*b + w*c, u = va * denom = 1.0f-v-w
        }

        public void DrawPlayer()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                        DrawDebugTileBox(i, j, k);

            //DrawDebugTileBox(0, 1, 1);
            //DrawDebugTileBox(2, 1, 1);
            //DrawDebugTileBox(1, 0, 1);
            //DrawDebugTileBox(1, 2, 1);
            //DrawDebugTileBox(1, 1, 0);
            //DrawDebugTileBox(1, 1, 2);

            if (debugVector3 != Vector3.Zero)
            {
                Matrix ScalePosBounds = Matrix.CreateScale(0.1f) * Matrix.CreateTranslation(debugVector3);
                rendering.DebugCube.Draw(rendering.WorldMatrix * ScalePosBounds, rendering.ViewMatrix, rendering.ProjectionMatrix);
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
                rendering.DebugCube.Draw(rendering.WorldMatrix * ScalePosBounds, rendering.ViewMatrix, rendering.ProjectionMatrix);
            }
        }
    }
}