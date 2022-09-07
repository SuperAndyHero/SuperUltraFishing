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
        public BasicEffect FlatColorEffect;
        //public Effect FlatColorEffect;

        //public List<VertexPositionColorTexture> TileMeshVertices = new();
        //public VertexBuffer VertBuffer;

        public Dictionary<Texture2D, List<VertexPositionColorNormalTexture>> TextureVertices = new Dictionary<Texture2D, List<VertexPositionColorNormalTexture>>();
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

        public HashSet<ushort> FourSidedTiles;

        //public HashSet<ushort> FourSidedTilesBL;
        public HashSet<ushort> CrossTile;

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
                        Texture = Terraria.GameContent.TextureAssets.BlackTile.Value,
                        LightingEnabled = true
                    };
                    BasicEffect.Projection = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 2f, (float)Main.screenWidth / (float)Main.screenHeight, 1, 2000);
                    BasicEffect.World = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up) * Matrix.CreateScale(10);


                    FlatColorEffect = new BasicEffect(Main.graphics.GraphicsDevice)
                    {
                        VertexColorEnabled = true,
                        LightingEnabled = true
                        //TextureEnabled = true,
                        //Texture = Terraria.GameContent.TextureAssets.BlackTile.Value
                    };
                    FlatColorEffect.Projection = BasicEffect.Projection;
                    FlatColorEffect.World = BasicEffect.World;


                    //FlatColorEffect = Mod.Assets.Request<Effect>("Effects/FlatColor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                    //FlatColorEffect.Parameters["World"].SetValue(BasicEffect.World);
                    //FlatColorEffect.Parameters["Projection"].SetValue(BasicEffect.Projection);
                    //FlatColorEffect.Parameters["Color"].SetValue(Color.Green.ToVector4());
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
                Main.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;//needed or earlier drawn tiles appear in front
                Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;//needed or a similar issue as above happens
                Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;//keeps the quads pixel perfect

                BasicEffect.View = FlatColorEffect.View = Matrix.CreateLookAt(CameraPosition, CameraPosition + Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(CameraYaw, CameraPitch, 0)), Vector3.Up);

                //todo: http://www.catalinzima.com/xna/tutorials/deferred-rendering-in-xna/point-lights/

                BasicEffect.FogEnabled = FlatColorEffect.FogEnabled = false;
                //BasicEffect.FogEnabled = FlatColorEffect.FogEnabled = true;
                BasicEffect.FogColor = FlatColorEffect.FogColor = new Vector3(0.12f, 0.12f, 0.35f);
                BasicEffect.FogStart = FlatColorEffect.FogStart = 0;
                BasicEffect.FogEnd = FlatColorEffect.FogEnd = 200;

                BasicEffect.AmbientLightColor = new Vector3(0.3f, 0.3f, 0.3f);

                BasicEffect.DirectionalLight0.Enabled = FlatColorEffect.DirectionalLight0.Enabled = true;
                BasicEffect.DirectionalLight0.SpecularColor = FlatColorEffect.DirectionalLight0.SpecularColor = Color.Gray.ToVector3();
                BasicEffect.DirectionalLight0.DiffuseColor = FlatColorEffect.DirectionalLight0.DiffuseColor = Color.Gray.ToVector3();
                BasicEffect.DirectionalLight0.Direction = FlatColorEffect.DirectionalLight0.Direction = Vector3.Normalize(Vector3.Down + Vector3.Left);

                foreach (KeyValuePair<Texture2D, VertexBuffer> pair in TextureBuffers)
                {
                    Main.graphics.GraphicsDevice.SetVertexBuffer(pair.Value);

                    FlatColorEffect.AmbientLightColor = TextureColor[pair.Key].ToVector3() * BasicEffect.AmbientLightColor;
                    FlatColorEffect.CurrentTechnique.Passes[0].Apply();
                    Main.graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, pair.Value.VertexCount / 3);
                    //var verArray = TextureVertices[pair.Key].ToArray();
                    //Main.graphics.GraphicsDevice.DrawUserPrimitives<VertexPositionColorTexture>(PrimitiveType.TriangleList, verArray, 0, pair.Value.VertexCount / 3);

                    BasicEffect.Texture = pair.Key;
                    BasicEffect.CurrentTechnique.Passes[0].Apply();
                    Main.graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, pair.Value.VertexCount / 3);
                    //Main.graphics.GraphicsDevice.DrawUserPrimitives<VertexPositionColorTexture>(PrimitiveType.TriangleList, verArray, 0, pair.Value.VertexCount / 3);
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


            foreach (KeyValuePair<Texture2D, List<VertexPositionColorNormalTexture>> pair in TextureVertices)
            {
                TextureBuffers.Add(pair.Key, new VertexBuffer(Main.graphics.GraphicsDevice, typeof(VertexPositionColorNormalTexture), pair.Value.Count, BufferUsage.None));
                var vertArray = pair.Value.ToArray();
                CalculateArrayNormals(vertArray);
                TextureBuffers[pair.Key].SetData(vertArray);
            }

            VertexBufferBuilt = true;
        }

        public static void CalculateArrayNormals(VertexPositionColorNormalTexture[] list)
        {
            for (int i = 0; i < list.Length; i += 3)
            {
                Vector3 normal = CalculateNormal(list[i].Position, list[i + 1].Position, list[i + 2].Position);
                list[i].Normal = normal;
                list[i + 1].Normal = normal;
                list[i + 2].Normal = normal;
            }
        }
        public static Vector3 CalculateNormal(Vector3 pos1, Vector3 pos2, Vector3 pos3)
        {
            Vector3 side1 = pos1 - pos3;
            Vector3 side2 = pos1 - pos2;
            return Vector3.Normalize(Vector3.Cross(side1, side2));
        }

        public class TileState
        {
            public (bool active, Vector2 Frame) TopFace;
            public (bool active, Vector2 Frame) BottomFace;
            public (bool active, Vector2 Frame) FrontFace;
            public (bool active, Vector2 Frame) BackFace;
            public (bool active, Vector2 Frame) RightFace;
            public (bool active, Vector2 Frame) LeftFace;
            public bool CrossTile;
            public TileState(Vector2 BaseFrame)
            {
                TopFace = BottomFace = FrontFace = BackFace = RightFace = LeftFace = (true, BaseFrame);
                CrossTile = false;
            }
        }

        private TileState FrameTile(BasicTile tile, int x, int y, int z)
        {
            TileState tileState = new TileState(tile.TileFrame);

            int sizeX = world.AreaArray.GetLength(0);
            int sizeY = world.AreaArray.GetLength(1);
            int sizeZ = world.AreaArray.GetLength(2);

            tileState.RightFace.active = !(x + 1 < sizeX) || !world.AreaArray[x + 1, y, z].Active || !world.AreaArray[x + 1, y, z].Collide;
            tileState.LeftFace.active = !(x - 1 >= 0) || !world.AreaArray[x - 1, y, z].Active || !world.AreaArray[x - 1, y, z].Collide;
            tileState.TopFace.active = !(y + 1 < sizeY) || !world.AreaArray[x, y + 1, z].Active || !world.AreaArray[x, y + 1, z].Collide;
            tileState.BottomFace.active = !(y - 1 >= 0) || !world.AreaArray[x, y - 1, z].Active || !world.AreaArray[x, y - 1, z].Collide;
            tileState.FrontFace.active = !(z + 1 < sizeZ) || !world.AreaArray[x, y, z + 1].Active || !world.AreaArray[x, y, z + 1].Collide;
            tileState.BackFace.active = !(z - 1 >= 0) || !world.AreaArray[x, y, z - 1].Active || !world.AreaArray[x, y, z - 1].Collide;

            tileState.CrossTile = world.CrossTile.Contains(tile.TileType);

            //sand is broken due to it falling
            if (!(Main.tileFrameImportant[tile.TileType] || tileState.CrossTile))//does not touch frame important tiles since they already have their frame and this would break them
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int offX = x + (i - 1);   
                        int offY = y + ((-j + 2) - 1);
                        int worldx = i + 5;
                        int worldy = j + 5;
                        if (!(offX < 0 || offX >= sizeX || offY < 0 || offY >= sizeY))
                        {
                            //WorldGen.PlaceTile(worldx, worldy, world.AreaArray[offX, offY, z].TileType, true, true);//much slower
                            Main.tile[worldx, worldy].Get<TileWallWireStateData>().HasTile = world.AreaArray[offX, offY, z].Active;
                            Main.tile[worldx, worldy].Get<TileTypeData>().Type = world.AreaArray[offX, offY, z].TileType;
                            //Main.tile[worldx, worldy].Get<TileWallWireStateData>().TileFrameX = (short)world.AreaArray[offX, offY, z].TileFrame.X;
                            //Main.tile[worldx, worldy].Get<TileWallWireStateData>().TileFrameY = (short)world.AreaArray[offX, offY, z].TileFrame.Y;
                        }
                        else
                            Main.tile[worldx, worldy].Get<TileWallWireStateData>().HasTile = false;
                    }
                }
                WorldGen.TileFrame(6, 6, true);
                tileState.FrontFace.Frame = tileState.BackFace.Frame = new Vector2(Main.tile[6, 6].TileFrameX, Main.tile[6, 6].TileFrameY);


                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int offZ = z + (i - 1);
                        int offY = y + ((-j + 2) - 1);
                        int worldx = i + 5;
                        int worldy = j + 5;
                        if (!(offY < 0 || offY >= sizeY || offZ < 0 || offZ >= sizeZ))
                        {
                            //WorldGen.PlaceTile(worldx, worldy, world.AreaArray[x, offY, offZ].TileType, true, true);
                            Main.tile[worldx, worldy].Get<TileWallWireStateData>().HasTile = world.AreaArray[x, offY, offZ].Active;
                            Main.tile[worldx, worldy].Get<TileTypeData>().Type = world.AreaArray[x, offY, offZ].TileType;
                            //Main.tile[worldx, worldy].Get<TileWallWireStateData>().TileFrameX = (short)world.AreaArray[x, offY, offZ].TileFrame.X;
                            //Main.tile[worldx, worldy].Get<TileWallWireStateData>().TileFrameY = (short)world.AreaArray[x, offY, offZ].TileFrame.Y;
                        }
                        else
                            Main.tile[worldx, worldy].Get<TileWallWireStateData>().HasTile = false;
                    }
                }
                WorldGen.TileFrame(6, 6, true);
                tileState.RightFace.Frame = tileState.LeftFace.Frame = new Vector2(Main.tile[6, 6].TileFrameX, Main.tile[6, 6].TileFrameY);


                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int offX = x + (i - 1);
                        int offZ = z + (j - 1);
                        int worldx = i + 5;
                        int worldy = j + 5;
                        if (!(offX < 0 || offX >= sizeX || offZ < 0 || offZ >= sizeZ))
                        {
                            //WorldGen.PlaceTile(worldx, worldy, world.AreaArray[offX, y, offZ].TileType, true, true);
                            Main.tile[worldx, worldy].Get<TileWallWireStateData>().HasTile = world.AreaArray[offX, y, offZ].Active;
                            Main.tile[worldx, worldy].Get<TileTypeData>().Type = world.AreaArray[offX, y, offZ].TileType;
                            //Main.tile[worldx, worldy].Get<TileWallWireStateData>().TileFrameX = (short)world.AreaArray[offX, y, offZ].TileFrame.X;
                            //Main.tile[worldx, worldy].Get<TileWallWireStateData>().TileFrameY = (short)world.AreaArray[offX, y, offZ].TileFrame.Y;
                        }
                        else
                            Main.tile[worldx, worldy].Get<TileWallWireStateData>().HasTile = false;

                        Terraria.ObjectData.TileObjectData.newTile.CopyFrom(Terraria.ObjectData.TileObjectData.Style1xX);
                    }
                }
                WorldGen.TileFrame(6, 6, true);
                tileState.TopFace.Frame = tileState.BottomFace.Frame = new Vector2(Main.tile[6, 6].TileFrameX, Main.tile[6, 6].TileFrameY);
            }
            else
            {
                tileState.TopFace.Frame = tileState.BottomFace.Frame = new Vector2(ushort.MaxValue);

                //is not a quad tile blank out 2 sides of the tile
                if (!world.FourSidedTiles.Contains(tile.TileType))
                {
                    tileState.RightFace.Frame = tileState.LeftFace.Frame = new Vector2(ushort.MaxValue);
                }
            }


            return tileState;
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

                        //if this texture does not exist in the dictionary add a new entry
                        if (!TextureColor.ContainsKey(tileTexture))
                        {
                            int ltile = Terraria.Map.MapHelper.tileLookup[tile.TileType];
                            if (ltile >= colorLookup.Length)
                                ltile = tile.TileType;

                            TextureColor[tileTexture] = colorLookup[ltile];
                        }

                        TileState tileState = FrameTile(tile, x, y, z);//lighting may need to be included in this later

                        if (!tileState.CrossTile)
                        {
                            //right
                            if (tileState.RightFace.active)
                                AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, -(float)Math.PI / 2), brightnessColor, tileTexture, tileState.RightFace.Frame, SpriteEffects.FlipHorizontally);

                            //left
                            if (tileState.LeftFace.active)
                                AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, (float)Math.PI / 2), brightnessColor, tileTexture, tileState.LeftFace.Frame);

                            //top
                            if (tileState.TopFace.active)
                                AddQuad(new Vector3(x, y, z), new Vector3(0, 0, 0), brightnessColor, tileTexture, tileState.TopFace.Frame);

                            //bottom
                            if (tileState.BottomFace.active)
                                AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI, 0), brightnessColor, tileTexture, tileState.BottomFace.Frame);

                            //front
                            if (tileState.FrontFace.active)
                                AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, 0), brightnessColor, tileTexture, tileState.FrontFace.Frame);

                            //back
                            if (tileState.BackFace.active)
                                AddQuad(new Vector3(x, y, z), new Vector3((float)Math.PI / 2, (float)Math.PI / 2, -(float)Math.PI / 2), brightnessColor, tileTexture, tileState.BackFace.Frame, SpriteEffects.FlipHorizontally);
                        }
                    }
                }
            }
        }

        private void AddQuad(Vector3 position, Vector3 ypr, Color color, Texture2D texture, Vector2 frame = default, SpriteEffects effects = SpriteEffects.None)
        {
            float xSize = 1f / texture.Width;
            float ySize = 1f / texture.Height;

            if (!TextureVertices.ContainsKey(texture))
                TextureVertices[texture] = new List<VertexPositionColorNormalTexture>();

            float xMin = frame.X * xSize;
            float xMax = xMin + (16 * xSize);

            float yMin = frame.Y * ySize;
            float yMax = yMin + (16 * ySize);

            if (effects.HasFlag(SpriteEffects.FlipHorizontally))
                (xMin, xMax) = (xMax, xMin);

            if (effects.HasFlag(SpriteEffects.FlipVertically))
                (yMin, yMax) = (yMax, yMin);

            VertexPositionColorNormalTexture vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(-0.5f, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMin, yMin));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(0.5f, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMax, yMin));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(-0.5f, 0.5f, 0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMin, yMax));
            TextureVertices[texture].Add(vertex);


            vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(0.5f, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMax, yMin));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(0.5f, 0.5f, 0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMax, yMax));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(-0.5f, 0.5f, 0.5f), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMin, yMax));
            TextureVertices[texture].Add(vertex);
        }

        private void AddFloorPlane(Texture2D texture)
        {
            if (!TextureVertices.ContainsKey(texture))
                TextureVertices[texture] = new List<VertexPositionColorNormalTexture>();

            if (!TextureColor.ContainsKey(texture))
                TextureColor[texture] = Color.WhiteSmoke;

            float scale = 100;
            float drop = 32;

            VertexPositionColorNormalTexture vertex = new VertexPositionColorNormalTexture(
                new Vector3(-scale, -drop, -scale),
                Color.BlanchedAlmond,
                new Vector2(0, 0));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                new Vector3(scale, -drop, -scale),
                Color.Cornsilk,
                new Vector2(1, 0));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                new Vector3(-scale, -drop, scale),
                Color.Cornsilk,
                new Vector2(0, 1));
            TextureVertices[texture].Add(vertex);




            vertex = new VertexPositionColorNormalTexture(
                new Vector3(scale, -drop, -scale),
                Color.Cornsilk,
                new Vector2(1, 0));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                new Vector3(scale, -drop, scale),
                Color.SaddleBrown,
                 new Vector2(1, 1));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                new Vector3(-scale, -drop, scale),
                Color.Cornsilk,
                new Vector2(0, 1));
            TextureVertices[texture].Add(vertex);
        }
    }
}
