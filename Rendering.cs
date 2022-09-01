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
        public BasicEffect BasicEffect;
        public Effect FlatColorEffect;

        //public List<VertexPositionColorTexture> TileMeshVertices = new();
        //public VertexBuffer VertBuffer;

        public Dictionary<Texture2D, List<VertexPositionColorTexture>> TextureVertices = new Dictionary<Texture2D, List<VertexPositionColorTexture>>();
        public Dictionary<Texture2D, VertexBuffer> TextureBuffers = new Dictionary<Texture2D, VertexBuffer>();
        public Dictionary<Texture2D, Color> TextureColor = new Dictionary<Texture2D, Color>();
        //this could use a 1x1 texture instead, which would reduce the amount of SetVertexBuffer, and needing to change the rasterizer state

        public RasterizerState FlatColorRasterizer = new RasterizerState() { };
        public RasterizerState TexturedRasterizer = new RasterizerState() { DepthBias = -0.0001f };

        public Vector3 CameraPosition = Vector3.Zero;
        public float CameraYaw = 0;
        public float CameraPitch = 0;

        public bool VertexBufferBuilt = false;

        private World world;
        private FishingUIWindow fishingUIWindow;

        public static Color[] colorLookup;

        public override void PostAddRecipes()
        {
            world = GetInstance<World>();
            fishingUIWindow = GetInstance<FishingUIWindow>();
        }
        public override void Load()
        {
            FieldInfo fieldInfo = typeof(Terraria.Map.MapHelper).GetField("colorLookup", BindingFlags.NonPublic | BindingFlags.Static);
            colorLookup = (Color[])fieldInfo.GetValue(null);

            Main.QueueMainThreadAction(() =>
            {
                if (!Main.dedServ)
                {
                    WindowTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, default, DepthFormat.Depth24Stencil8);
                    
                    BasicEffect = new BasicEffect(Main.graphics.GraphicsDevice)
                    {
                        VertexColorEnabled = true,
                        TextureEnabled = true,
                        Texture = Terraria.GameContent.TextureAssets.BlackTile.Value
                    };
                    BasicEffect.Projection = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 2f, (float)Main.screenWidth / (float)Main.screenHeight, 1, 2000);
                    BasicEffect.World = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up) * Matrix.CreateScale(10);

                    FlatColorEffect = Mod.Assets.Request<Effect>("Effects/FlatColor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                    FlatColorEffect.Parameters["World"].SetValue(BasicEffect.World);
                    FlatColorEffect.Parameters["Projection"].SetValue(BasicEffect.Projection);
                    FlatColorEffect.Parameters["Color"].SetValue(Color.Green.ToVector4());
                }
            });
        }

        public void ResetCamera()
        {
            CameraPosition = Vector3.Zero;
            CameraYaw = 0;
            CameraPitch = 0;
        }

        //draw vertex buffer to render target
        public override void PostDrawTiles()
        {
            if (fishingUIWindow.WindowActive)
            {
                if (!VertexBufferBuilt)
                    return;

                //Main.NewText("yaw: " + CameraYaw);
                //Main.NewText("pitch: " + CameraPitch);

                Main.graphics.GraphicsDevice.SetRenderTarget(WindowTarget);
                Main.graphics.GraphicsDevice.Clear(Color.Gray);
                Main.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
                Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

                //these can be used to flat color tiles if fog end is close
                //BasicEffect.FogEnabled = true;
                //BasicEffect.FogColor = Color.Blue.ToVector3();


                //Terraria.GameContent.TextureAssets.Ninja.Value;

                BasicEffect.View = Matrix.CreateLookAt(CameraPosition, CameraPosition + Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(CameraYaw, CameraPitch, 0)), Vector3.Up);
                FlatColorEffect.Parameters["View"].SetValue(BasicEffect.View);

                BasicEffect.FogEnabled = true;
                BasicEffect.FogColor = Color.DarkBlue.ToVector3();
                BasicEffect.FogStart = 100;
                BasicEffect.FogEnd = 1000;

                Main.graphics.GraphicsDevice.RasterizerState = FlatColorRasterizer;

                foreach (KeyValuePair<Texture2D, VertexBuffer> pair in TextureBuffers)
                {
                    FlatColorEffect.Parameters["Color"].SetValue(TextureColor[pair.Key].ToVector4());
                    FlatColorEffect.CurrentTechnique.Passes[0].Apply();
                    Main.graphics.GraphicsDevice.SetVertexBuffer(pair.Value);
                    Main.graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, pair.Value.VertexCount / 3);
                }

                Main.graphics.GraphicsDevice.RasterizerState = TexturedRasterizer;

                foreach (KeyValuePair<Texture2D, VertexBuffer> pair in TextureBuffers)
                {
                    BasicEffect.Texture = pair.Key;
                    BasicEffect.CurrentTechnique.Passes[0].Apply();
                    Main.graphics.GraphicsDevice.SetVertexBuffer(pair.Value);
                    Main.graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, pair.Value.VertexCount / 3);
                }

                Main.graphics.GraphicsDevice.SetRenderTarget(null);
            }
        }

        public void BuildVertexBuffer()
        {
            TextureVertices.Clear();
            TextureBuffers.Clear();
            TextureColor.Clear();


            AddFloorPlane(Terraria.GameContent.TextureAssets.Ninja.Value);

            BuildTileMesh();
             

            foreach(KeyValuePair<Texture2D, List<VertexPositionColorTexture>> pair in TextureVertices)
            {
                TextureBuffers.Add(pair.Key, new VertexBuffer(Main.graphics.GraphicsDevice, typeof(VertexPositionColorTexture), pair.Value.Count, BufferUsage.WriteOnly));
                TextureBuffers[pair.Key].SetData(pair.Value.ToArray());
            }

            VertexBufferBuilt = true;
        }

        private void BuildTileMesh()//todo: maybe bake the lighting in?
        {
            if (!world.WorldGenerated)
            {
                Main.NewText("Tried to build world before generated", Color.IndianRed);
                return;
            }

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

                        Main.instance.LoadTiles(tile.TileType);//ensures the tile is loaded, likely not needed in normal gameplay
                        Texture2D tileTexture = Terraria.GameContent.TextureAssets.Tile[tile.TileType].Value;


                        float colorMult = 1f;// new Vector3(x, y, z).Length() / new Vector3(sizeX, sizeY, sizeZ).Length();
                        Color brightnessColor = Color.White * colorMult;

                        if (!TextureColor.ContainsKey(tileTexture))
                        {
                            int ltile = Terraria.Map.MapHelper.tileLookup[tile.TileType];
                            if (ltile >= colorLookup.Length)
                                ltile = tile.TileType;

                            TextureColor[tileTexture] = colorLookup[ltile] * colorMult;
                        }


                        //right
                        if (!(x + 1 < sizeX) || !world.AreaArray[x + 1, y, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, -(float)Math.PI / 2), brightnessColor, tileTexture, tile.TileFrame);

                        //left
                        if (!(x - 1 >= 0) || !world.AreaArray[x - 1, y, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, (float)Math.PI / 2), brightnessColor, tileTexture, tile.TileFrame);

                        //top
                        if (!(y + 1 < sizeY) || !world.AreaArray[x, y + 1, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, 0, 0), brightnessColor, tileTexture, tile.TileFrame);

                        //bottom
                        if (!(y - 1 >= 0) || !world.AreaArray[x, y - 1, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI, 0), brightnessColor, tileTexture, tile.TileFrame);

                        //front (correct)
                        if (!(z + 1 < sizeZ) || !world.AreaArray[x, y, z + 1].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, 0), brightnessColor, tileTexture, tile.TileFrame);

                        //back (correct)
                        if (!(z - 1 >= 0) || !world.AreaArray[x, y, z - 1].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3((float)Math.PI / 2, (float)Math.PI / 2, -(float)Math.PI / 2), brightnessColor, tileTexture, tile.TileFrame, SpriteEffects.FlipHorizontally);
                    }
                }
            }
        }

        private void AddQuad(Vector3 position, Vector3 ypr, Color color, Texture2D texture, Vector2 frame = default, SpriteEffects effects = SpriteEffects.None)
        {
            float xSize = 1f / texture.Width;
            float ySize = 1f / texture.Height;

            if (!TextureVertices.ContainsKey(texture))
                TextureVertices[texture] = new List<VertexPositionColorTexture>();

            float xMin = frame.X * xSize;
            float xMax = xMin + (16 * xSize);

            float yMin = frame.Y * ySize;
            float yMax = yMin + (16 * ySize);

            if (effects.HasFlag(SpriteEffects.FlipHorizontally))
                (xMin, xMax) = (xMax, xMin);

            if (effects.HasFlag(SpriteEffects.FlipVertically))
                (yMin, yMax) = (yMax, yMin);

            VertexPositionColorTexture vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(-0.5f, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(xMin, yMin);
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(0.5f, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(xMax, yMin);
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(-0.5f, 0.5f, 0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(xMin, yMax);
            TextureVertices[texture].Add(vertex);


            vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(0.5f, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(xMax, yMin);
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(0.5f, 0.5f, 0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(xMax, yMax);
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = position + Vector3.Transform(new Vector3(-0.5f, 0.5f, 0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z));
            vertex.Color = color;
            vertex.TextureCoordinate = new Vector2(xMin, yMax);

            TextureVertices[texture].Add(vertex);
        }

        private void AddFloorPlane(Texture2D texture)
        {
            if (!TextureVertices.ContainsKey(texture))
                TextureVertices[texture] = new List<VertexPositionColorTexture>();

            if (!TextureColor.ContainsKey(texture))
                TextureColor[texture] = Color.WhiteSmoke;

            float scale = 100;
            float drop = 32;
            VertexPositionColorTexture vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(-scale, -drop, -scale);
            vertex.Color = Color.BlanchedAlmond;
            vertex.TextureCoordinate = new Vector2(0, 0);
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(scale, -drop, -scale);
            vertex.Color = Color.Cornsilk;
            vertex.TextureCoordinate = new Vector2(1, 0);
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(-scale, -drop, scale);
            vertex.Color = Color.Cornsilk;
            vertex.TextureCoordinate = new Vector2(0, 1);
            TextureVertices[texture].Add(vertex);


            vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(scale, -drop, -scale);
            vertex.Color = Color.Cornsilk;
            vertex.TextureCoordinate = new Vector2(1, 0);
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(scale, -drop, scale);
            vertex.Color = Color.SaddleBrown;
            vertex.TextureCoordinate = new Vector2(1, 1);
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorTexture();
            vertex.Position = new Vector3(-scale, -drop, scale);
            vertex.Color = Color.Cornsilk;
            vertex.TextureCoordinate = new Vector2(0, 1);
            TextureVertices[texture].Add(vertex);
        }
    }
}
