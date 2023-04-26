using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Terraria.ModLoader.PlayerDrawLayer;

namespace SuperUltraFishing
{
    public static class Collision
    {
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

        public static Vector3 CollideSphereWithTile(BoundingSphere BoundingSphere, int i, int j, int k, World world, out bool Collided)
        {
            Vector3 StartPosition = BoundingSphere.Center;
            int tilePosX = (int)((StartPosition.X / 10f) - 0.5f) + i;
            int tilePosY = (int)((StartPosition.Y / 10f) - 0.5f) + j;
            int tilePosZ = (int)((StartPosition.Z / 10f) - 0.5f) + k;

            bool collided2 = false;

            if (world.ValidTilePos(tilePosX, tilePosY, tilePosZ) && world.TempCollisionType(tilePosX, tilePosY, tilePosZ) == 1)
            {
                //List<(Vector3 off, Vector3 closestPoint)> CollisionList = new List<(Vector3 off, Vector3 closestPoint)>();

                //OPIMIZATION TODO: since this checks which side is closest anyway, use of triangles could be removed completely
                void CheckTriangle(Triangle triangle)
                {
                    //clips into corners because block isnt checked
                    if (SphereIntersectsTriangle(BoundingSphere, triangle, out Vector3 closestPointOnTri))
                    {
                        StartPosition += triangle.Normal *
                            (Vector3.Distance(BoundingSphere.Center, closestPointOnTri) - BoundingSphere.Radius);

                        BoundingSphere.Center = StartPosition;
                        collided2 = true;
                        //Velocity *= 1f;//friction (help prevents studders when going over corner)
                    }
                }

                //this list is so that the closest side can be moved to the front, this prevents one side taking priority
                const float defaultDist = 100000;
                float lastDistToCenter = defaultDist;
                List<(Triangle triA, Triangle triB)> CollisionFaceList = new List<(Triangle triA, Triangle triB)>();

                //up
                if (world.ValidTilePos(tilePosX, tilePosY + 1, tilePosZ) && world.TempCollisionType(tilePosX, tilePosY + 1, tilePosZ) != 1)
                {
                    float distance = Vector3.Distance(new Vector3(tilePosX, tilePosY + 0.5f, tilePosZ) * 10, StartPosition);
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
                    float distance = Vector3.Distance(new Vector3(tilePosX, tilePosY - 0.5f, tilePosZ) * 10, StartPosition);
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
                    float distance = Vector3.Distance(new Vector3(tilePosX + 0.5f, tilePosY, tilePosZ) * 10, StartPosition);
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
                    float distance = Vector3.Distance(new Vector3(tilePosX - 0.5f, tilePosY, tilePosZ) * 10, StartPosition);
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
                    float distance = Vector3.Distance(new Vector3(tilePosX, tilePosY, tilePosZ + 0.5f) * 10, StartPosition);
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
                    float distance = Vector3.Distance(new Vector3(tilePosX, tilePosY, tilePosZ - 0.5f) * 10, StartPosition);
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
            Collided = collided2;
            return BoundingSphere.Center;
        }

        public static bool SphereIntersectsTriangle(BoundingSphere sphere, Triangle triangle, out Vector3 point) =>
            SphereIntersectsTriangle(sphere, triangle.PointA, triangle.PointB, triangle.PointC, out point);
        public static bool SphereIntersectsTriangle(BoundingSphere sphere, Vector3 a, Vector3 b, Vector3 c, out Vector3 point)
        {
            // Find point P on triangle ABC closest to sphere center
            point = ClosestPointOnTriangle(sphere.Center, a, b, c);
            // Sphere and triangle intersect if the (squared) distance from sphere
            // center to point p is less than the (squared) sphere radius
            Vector3 v = point - sphere.Center;
            return Vector3.Dot(v, v) <= sphere.Radius * sphere.Radius;
        }
        public static Vector3 ClosestPointOnTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
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

        public static Vector3 CollideSphrWithSphr(Vector3 SphrACenter, float SphrARad, Vector3 SphrBCenter, float SphrBRad)
        {
            Vector3 dirVector = Vector3.Normalize(SphrACenter - SphrBCenter);
            float dist = Vector3.Distance(SphrACenter, SphrBCenter);
            float offDist = (SphrARad + SphrBRad) - dist;

            return (dirVector * (offDist * 0.5f));//an offset to be applied to both spheres
            //this could be changed to take sphere size or mass into account and return 2 offsets
        }
    }
}
