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
    internal class Rendering : ModSystem
    {
        public RenderTarget2D WindowTarget;
        public BasicEffect basicEffect;

        public List<VertexPositionColorTexture> TileMeshVertices = new();
        public VertexBuffer VertBuffer;

        public Vector3 CameraPosition = Vector3.Zero;
        public float CameraYaw = 0;
        public float CameraPitch = 0;

        public bool VertexBufferBuilt = false;

        private World world;

        public override void Load()
        {
            world = GetInstance<World>();

            Main.QueueMainThreadAction(() =>
            {
                if (!Main.dedServ)
                {
                    WindowTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, default, DepthFormat.Depth24Stencil8);
                    basicEffect = new BasicEffect(Main.graphics.GraphicsDevice)
                    {
                        VertexColorEnabled = true,
                        TextureEnabled = true,
                        Texture = Terraria.GameContent.TextureAssets.Ninja.Value
                    };
                    basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 2f, (float)Main.screenWidth / (float)Main.screenHeight, 1, 2000);
                    basicEffect.World = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up) * Matrix.CreateScale(10);
                }
            });
        }

        public override void PostDrawTiles()
        {
            if (!VertexBufferBuilt)
                BuildVertexBuffer();

            Main.graphics.GraphicsDevice.SetRenderTarget(WindowTarget);
            Main.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            if (VertexBufferBuilt)
            {
                basicEffect.View = Matrix.CreateLookAt(CameraPosition, CameraPosition + Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(CameraYaw, CameraPitch, 0)), Vector3.Up);
                //Main.NewText("yaw: " + CameraYaw);
                //Main.NewText("pitch: " + CameraPitch);

                basicEffect.CurrentTechnique.Passes[0].Apply();
                Main.graphics.GraphicsDevice.SetVertexBuffer(VertBuffer);
                Main.graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, VertBuffer.VertexCount / 3);
            }
            Main.graphics.GraphicsDevice.SetRenderTarget(null);
        }

        public void BuildVertexBuffer()
        {
            TileMeshVertices.Clear();
            
            BuildTileMesh();

            AddFloorPlane();

            VertBuffer = new VertexBuffer(Main.graphics.GraphicsDevice, typeof(VertexPositionColorTexture), TileMeshVertices.Count + 1, BufferUsage.WriteOnly);
            VertBuffer.SetData(TileMeshVertices.ToArray());

            VertexBufferBuilt = true;
        }

        public void BuildTileMesh()
        {
            int sizeX = world.AreaArray.GetLength(0);
            int sizeY = world.AreaArray.GetLength(1);
            int sizeZ = world.AreaArray.GetLength(2);

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        BasicTile tile = world.AreaArray[x, y, z];
                        if (!tile.Active)
                            continue;

                        float colorMult = 1f;// new Vector3(x, y, z).Length() / new Vector3(sizeX, sizeY, sizeZ).Length();

                        if (!(x + 1 < sizeZ) || !world.AreaArray[x + 1, y, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, 0, -(float)Math.PI / 2), Color.Purple * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);

                        if (!(x - 1 >= 0) || !world.AreaArray[x - 1, y, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, 0, (float)Math.PI / 2), Color.Green * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);

                        if (!(y + 1 < sizeZ) || !world.AreaArray[x, y + 1, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, 0, 0), Color.White * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);


                        if (!(y - 1 >= 0) || !world.AreaArray[x, y - 1, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI, 0), Color.Red * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);

                        if (!(z + 1 < sizeZ) || !world.AreaArray[x, y, z + 1].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, 0), Color.Yellow * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);

                        if (!(z - 1 >= 0) || !world.AreaArray[x, y, z - 1].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, -(float)Math.PI / 2, 0), Color.Blue * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);
                    }
                }
            }
        }

        public void AddFloorPlane()
        {
            float scale = 100;
            float drop = 32;
            VertexPositionColorTexture vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(-scale, -drop, -scale);
            vertex.Color = Color.BlanchedAlmond;
            vertex.TextureCoordinate = new Vector2(0, 0);
            TileMeshVertices.Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(scale, -drop, -scale);
            vertex.Color = Color.Cornsilk;
            vertex.TextureCoordinate = new Vector2(1, 0);
            TileMeshVertices.Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(-scale, -drop, scale);
            vertex.Color = Color.Cornsilk;
            vertex.TextureCoordinate = new Vector2(0, 1);
            TileMeshVertices.Add(vertex);


            vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(scale, -drop, -scale);
            vertex.Color = Color.Cornsilk;
            vertex.TextureCoordinate = new Vector2(1, 0);
            TileMeshVertices.Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(scale, -drop, scale);
            vertex.Color = Color.SaddleBrown;
            vertex.TextureCoordinate = new Vector2(1, 1);
            TileMeshVertices.Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(-scale, -drop, scale);
            vertex.Color = Color.Cornsilk;
            vertex.TextureCoordinate = new Vector2(0, 1);

            TileMeshVertices.Add(vertex);
        }

        public void AddQuad(Vector3 position, Vector3 ypr, Color color, Texture2D texture, Vector2 frame = default, SpriteEffects effects = SpriteEffects.None)
        {
            int Ycoord = effects.HasFlag(SpriteEffects.FlipVertically) ? 1 : 0;
            int Xcoord = effects.HasFlag(SpriteEffects.FlipHorizontally) ? 1 : 0;

            VertexPositionColorTexture vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(-0.5f, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(Xcoord, Ycoord);
            TileMeshVertices.Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(0.5f, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(1 - Xcoord, Ycoord);
            TileMeshVertices.Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(-0.5f, 0.5f, 0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(Xcoord, 1 - Ycoord);
            TileMeshVertices.Add(vertex);


            vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(0.5f, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(1 - Xcoord, Ycoord);
            TileMeshVertices.Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(0.5f, 0.5f, 0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(1 - Xcoord, 1 - Ycoord);
            TileMeshVertices.Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(-0.5f, 0.5f, 0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(Xcoord, 1 - Ycoord);

            TileMeshVertices.Add(vertex);
        }
    }
}
