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
using ReLogic.Content;

namespace SuperUltraFishing
{
    internal class Rendering : ModSystem
    {
        public RenderTarget2D WindowTarget;
        public RenderTarget2D WaterTarget;
        public BasicEffect BasicEffect;
        public BasicEffect FlatColorEffect;
        public AlphaTestEffect AlphaEffect;
        public Effect WaterShimmerEffect;
        public Effect WaterPostProcessEffect;
        //public Effect FlatColorEffect;

        public Texture2D LargePerlin;
        public Texture2D SmallPerlin;

        public Vector3 CameraPosition = Vector3.Zero;
        public float CameraYaw = 0;
        public float CameraPitch = 0;

        //public List<VertexPositionColorTexture> TileMeshVertices = new();
        //public VertexBuffer VertBuffer;

        public Dictionary<Texture2D, List<VertexPositionColorNormalTexture>> TextureVertices = new Dictionary<Texture2D, List<VertexPositionColorNormalTexture>>();
        public Dictionary<Texture2D, VertexBuffer> TextureBuffers = new Dictionary<Texture2D, VertexBuffer>();
        public Dictionary<Texture2D, Color> TextureColor = new Dictionary<Texture2D, Color>();
        //this could use a 1x1 texture instead, which would reduce the amount of SetVertexBuffer, and needing to change the rasterizer state

        public List<VertexPositionColorTexture> WaterBufferList = new List<VertexPositionColorTexture>();
        public VertexBuffer WaterBuffer;

        public RasterizerState FlatColorRasterizer = new RasterizerState() { };
        public RasterizerState TexturedRasterizer = new RasterizerState() { DepthBias = -0.0001f };

        public bool VertexBufferBuilt = false;

        private World world;
        private RobotPlayer player;
        private FishingUIWindow fishingUIWindow;

        public static Color[] colorLookup;

        public HashSet<ushort> FourSidedTiles;

        //public HashSet<ushort> FourSidedTilesBL;
        public HashSet<ushort> CrossTile;

        public override void PostAddRecipes()
        {
            world = GetInstance<World>();
            player = GetInstance<RobotPlayer>();
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
                    SmallPerlin = ModContent.Request<Texture2D>("SuperUltraFishing/Effects/SmallPerlin", AssetRequestMode.ImmediateLoad).Value;
                    LargePerlin = ModContent.Request<Texture2D>("SuperUltraFishing/Effects/LargePerlin", AssetRequestMode.ImmediateLoad).Value;

                    WindowTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, default, DepthFormat.Depth24Stencil8);
                    WaterTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, SurfaceFormat.Rg32, DepthFormat.Depth24Stencil8);

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

                    AlphaEffect = new AlphaTestEffect(Main.graphics.GraphicsDevice)
                    {
                        VertexColorEnabled = true,
                        Texture = Terraria.GameContent.TextureAssets.BlackTile.Value
                    };
                    AlphaEffect.Projection = BasicEffect.Projection;
                    AlphaEffect.World = BasicEffect.World;

                    WaterShimmerEffect = base.Mod.Assets.Request<Effect>("Effects/WaterShader", AssetRequestMode.ImmediateLoad).Value;
                    WaterPostProcessEffect = base.Mod.Assets.Request<Effect>("Effects/WaterPostProcess", AssetRequestMode.ImmediateLoad).Value;
                    //FlatColorEffect = Mod.Assets.Request<Effect>("Effects/FlatColor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                    //FlatColorEffect.Parameters["World"].SetValue(BasicEffect.World);
                    //FlatColorEffect.Parameters["Projection"].SetValue(BasicEffect.Projection);
                    //FlatColorEffect.Parameters["Color"].SetValue(Color.Green.ToVector4());
                }
            });
        }

        //draw vertex buffer to render target
        //public override void PostDrawInterface(SpriteBatch spriteBatch)
        //{
        //    base.PostDrawInterface(spriteBatch);
        //    Main.graphics.GraphicsDevice.RasterizerState.FillMode = FillMode.WireFrame;
        //}
        public override void PostDrawTiles()
        {
            if (fishingUIWindow.WindowActive)
            {
                if (!VertexBufferBuilt)
                    return;

                //seperated player and camera for future 
                CameraPosition = player.Position;
                CameraPitch = player.Pitch;
                CameraYaw = player.Yaw;

                //Main.NewText("yaw: " + CameraYaw);
                //Main.NewText("pitch: " + CameraPitch);

                //Main.graphics.GraphicsDevice.SetRenderTarget(WindowTarget);
                Main.graphics.GraphicsDevice.SetRenderTargets(WindowTarget, WaterTarget);
                Main.graphics.GraphicsDevice.Clear(Color.Transparent);
                Main.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;//needed or earlier drawn tiles appear in front
                Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;//needed or a similar issue as above happens
                Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;//keeps the quads pixel perfect

                BasicEffect.View = AlphaEffect.View = FlatColorEffect.View = Matrix.CreateLookAt(CameraPosition, CameraPosition + Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(CameraYaw, CameraPitch, 0)), Vector3.Up);

                //todo: http://www.catalinzima.com/xna/tutorials/deferred-rendering-in-xna/point-lights/

                BasicEffect.FogEnabled = AlphaEffect.FogEnabled = FlatColorEffect.FogEnabled = false;
                //BasicEffect.FogEnabled = AlphaEffect.FogEnabled = FlatColorEffect.FogEnabled = true;
                BasicEffect.FogColor = AlphaEffect.FogColor = FlatColorEffect.FogColor = new Vector3(0.12f, 0.12f, 0.35f);
                BasicEffect.FogStart = AlphaEffect.FogStart = FlatColorEffect.FogStart = 10;
                BasicEffect.FogEnd = AlphaEffect.FogEnd = FlatColorEffect.FogEnd = 250;

                BasicEffect.AmbientLightColor = new Vector3(0.3f, 0.3f, 0.3f);

                BasicEffect.DirectionalLight0.Enabled = FlatColorEffect.DirectionalLight0.Enabled = true;
                BasicEffect.DirectionalLight0.SpecularColor = FlatColorEffect.DirectionalLight0.SpecularColor = Color.Gray.ToVector3();
                BasicEffect.DirectionalLight0.DiffuseColor = AlphaEffect.DiffuseColor = FlatColorEffect.DirectionalLight0.DiffuseColor = Color.Gray.ToVector3();
                BasicEffect.DirectionalLight0.Direction = FlatColorEffect.DirectionalLight0.Direction = Vector3.Normalize(Vector3.Down + Vector3.Left);

                foreach (KeyValuePair<Texture2D, VertexBuffer> pair in TextureBuffers)
                {
                    Main.graphics.GraphicsDevice.SetVertexBuffer(pair.Value);
                    Color flatColor = TextureColor[pair.Key];
                    if (flatColor.A != 0)
                    {
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
                    else
                    {
                        //Main.graphics.GraphicsDevice.DepthStencilState.DepthBufferWriteEnable = false;
                        AlphaEffect.Texture = pair.Key;
                        AlphaEffect.CurrentTechnique.Passes[0].Apply();
                        AlphaEffect.AlphaFunction = CompareFunction.GreaterEqual;
                        AlphaEffect.ReferenceAlpha = 200;
                        Main.graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, pair.Value.VertexCount / 3);


                        //Main.graphics.GraphicsDevice.DepthStencilState.DepthBufferWriteEnable = false;
                        //Main.graphics.GraphicsDevice.DepthStencilState.DepthBufferFunction = CompareFunction.Always;
                        //still writes to depth buffer
                        //BasicEffect.Texture = pair.Key;
                        //BasicEffect.CurrentTechnique.Passes[0].Apply();
                        //Main.graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, pair.Value.VertexCount / 3);
                    }
                    //Main.graphics.GraphicsDevice.DepthStencilState.DepthBufferEnable = true;
                    //Main.graphics.GraphicsDevice.DepthStencilState.DepthBufferWriteEnable = true;
                    //Main.graphics.GraphicsDevice.DepthStencilState.DepthBufferFunction = CompareFunction.LessEqual;
                }

                //Main.graphics.GraphicsDevice.SetRenderTargets(WindowTarget, WaterTarget);

                //todo: fix the textures not looping
                WaterShimmerEffect.Parameters["WorldViewProjection"].SetValue((BasicEffect.World * BasicEffect.View) * BasicEffect.Projection);
                WaterShimmerEffect.Parameters["Offset"].SetValue(new Vector2(((float)Main.GameUpdateCount / 5000f) % 1));
                WaterShimmerEffect.Parameters["Strength"].SetValue(0.010f);

                WaterShimmerEffect.Parameters["DepthScale"].SetValue(0.00216f);//distance from camera, also effects shine

                WaterShimmerEffect.Parameters["ShineSize"].SetValue(25.0f);
                WaterShimmerEffect.Parameters["ShineLevel"].SetValue(0.968f);
                WaterShimmerEffect.Parameters["LargePerlinTexture"].SetValue((Texture)(object)LargePerlin);
                WaterShimmerEffect.Parameters["SmallPerlinTexture"].SetValue((Texture)(object)SmallPerlin);
                Main.graphics.GraphicsDevice.SetVertexBuffer(WaterBuffer);
                foreach (EffectPass pass2 in WaterShimmerEffect.CurrentTechnique.Passes)
                {
                    pass2.Apply();
                    Main.graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, WaterBuffer.VertexCount / 3);
                }

                Main.graphics.GraphicsDevice.SetRenderTarget(null);
            }
        }

        public void BuildVertexBuffer()
        {
            TextureVertices.Clear();
            TextureBuffers.Clear();
            TextureColor.Clear();
            WaterBufferList.Clear();

            AddFloorPlane(Terraria.GameContent.TextureAssets.Ninja.Value);

            BuildTileMesh();

            WaterBuffer = new VertexBuffer(Main.graphics.GraphicsDevice, typeof(VertexPositionColorTexture), WaterBufferList.Count, BufferUsage.None);
            WaterBuffer.SetData(WaterBufferList.ToArray());

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
            public TileState(Vector2 BaseFrame)
            {
                TopFace = BottomFace = FrontFace = BackFace = RightFace = LeftFace = (true, BaseFrame);
            }
        }

        private TileState FrameTile(BasicTile tile, int x, int y, int z)
        {
            TileState tileState = new TileState(tile.TileFrame);

            int sizeX = world.AreaArray.GetLength(0);
            int sizeY = world.AreaArray.GetLength(1);
            int sizeZ = world.AreaArray.GetLength(2);

            if (tile.Model != BasicTile.BlockModel.Cross && tile.Model != BasicTile.BlockModel.Extruded)
            {
                tileState.RightFace.active = !(x + 1 < sizeX) || !world.AreaArray[x + 1, y, z].Active || !world.AreaArray[x + 1, y, z].Collide || world.AreaArray[x + 1, y, z].Model != tile.Model;
                tileState.LeftFace.active = !(x - 1 >= 0) || !world.AreaArray[x - 1, y, z].Active || !world.AreaArray[x - 1, y, z].Collide || world.AreaArray[x - 1, y, z].Model != tile.Model;
                tileState.TopFace.active = !(y + 1 < sizeY) || !world.AreaArray[x, y + 1, z].Active || !world.AreaArray[x, y + 1, z].Collide || world.AreaArray[x, y + 1, z].Model != tile.Model;
                tileState.BottomFace.active = !(y - 1 >= 0) || !world.AreaArray[x, y - 1, z].Active || !world.AreaArray[x, y - 1, z].Collide || world.AreaArray[x, y - 1, z].Model != tile.Model;
                tileState.FrontFace.active = !(z + 1 < sizeZ) || !world.AreaArray[x, y, z + 1].Active || !world.AreaArray[x, y, z + 1].Collide || world.AreaArray[x, y, z + 1].Model != tile.Model;
                tileState.BackFace.active = !(z - 1 >= 0) || !world.AreaArray[x, y, z - 1].Active || !world.AreaArray[x, y, z - 1].Collide || world.AreaArray[x, y, z - 1].Model != tile.Model;


                //sand is broken due to it falling
                //solid tiles, does the expensive reframe check
                if (!Main.tileFrameImportant[tile.TileType])
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
                                Main.tile[worldx, worldy].Get<TileWallWireStateData>().HasTile = world.AreaArray[offX, offY, z].Active;
                                Main.tile[worldx, worldy].Get<TileTypeData>().Type = world.AreaArray[offX, offY, z].TileType;
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
                                Main.tile[worldx, worldy].Get<TileWallWireStateData>().HasTile = world.AreaArray[x, offY, offZ].Active;
                                Main.tile[worldx, worldy].Get<TileTypeData>().Type = world.AreaArray[x, offY, offZ].TileType;
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
                                Main.tile[worldx, worldy].Get<TileWallWireStateData>().HasTile = world.AreaArray[offX, y, offZ].Active;
                                Main.tile[worldx, worldy].Get<TileTypeData>().Type = world.AreaArray[offX, y, offZ].TileType;
                            }
                            else
                                Main.tile[worldx, worldy].Get<TileWallWireStateData>().HasTile = false;

                            Terraria.ObjectData.TileObjectData.newTile.CopyFrom(Terraria.ObjectData.TileObjectData.Style1xX);
                        }
                    }
                    WorldGen.TileFrame(6, 6, true);
                    tileState.TopFace.Frame = tileState.BottomFace.Frame = new Vector2(Main.tile[6, 6].TileFrameX, Main.tile[6, 6].TileFrameY);
                }
                //frameimportant tiles, blanks out some sides
                else
                {
                    tileState.TopFace.Frame = tileState.BottomFace.Frame = new Vector2(ushort.MaxValue);

                    //is not a quad tile blank out 2 sides of the tile
                    if (tile.Model != BasicTile.BlockModel.FourSidedCube)
                    {
                        tileState.RightFace.Frame = tileState.LeftFace.Frame = new Vector2(ushort.MaxValue);
                    }
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

            Texture2D tileTexture2 = Terraria.GameContent.TextureAssets.Background[279].Value;
            for (float i = 0; i < (float)Math.Tau; i += 0.5f)
            {
                float dist = 28f;
                Vector2 pos = new Vector2(dist, dist);
                pos = pos.RotatedBy(i, Vector2.Zero);
                AddQuad(new Vector3(pos.X, 0, pos.Y), new Vector3(-i - ((float)Math.PI * 0.25f), 0, (float)Math.PI / 2), Color.Gainsboro, new Vector2(30, 20), tileTexture2, new Rectangle(0, 0, tileTexture2.Width, tileTexture2.Height));
            }

            //AddWaterPlane(new Vector3(16, World.wallBuffer, 16), 0.5f, default, new Color(0.025f, 0.1f, 0.25f, 0.0f));
            const int WaterPlaneSize = 16;
            for (int x = 0; x < (sizeX); x += WaterPlaneSize)
            {
                for (int z = 0; z < (sizeZ); z += WaterPlaneSize)
                {
                    AddWaterPlane(new Vector3(x - 0.5f, sizeY - ((World.wallBuffer * 2) + 2.05f), (z + WaterPlaneSize) - 0.5f), WaterPlaneSize, new Vector3(0, (float)Math.PI, 0), new Color(0.045f, 0.2f, 0.45f, 0.0f));
                    ///*debug*/AddWaterPlane(new Vector3(x - 0.5f, sizeY - ((World.wallBuffer * 2)), z - 0.5f), WaterPlaneSize, new Vector3(0, 0, 0), new Color(0.025f, 0.1f, 0.25f, 0.0f));
                }
            }

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


                        TileState tileState = FrameTile(tile, x, y, z);//lighting may need to be included in this later

                        //if this texture does not exist in the dictionary add a new entry
                        if (!TextureColor.ContainsKey(tileTexture))
                        {
                            int ltile = Terraria.Map.MapHelper.tileLookup[tile.TileType];
                            if (ltile >= colorLookup.Length)
                                ltile = tile.TileType;

                            if(tile.Model != BasicTile.BlockModel.Cross && tile.Model != BasicTile.BlockModel.CubeTransparent && tile.Model != BasicTile.BlockModel.Extruded)
                                TextureColor[tileTexture] = colorLookup[ltile];
                            else
                                TextureColor[tileTexture] = Color.Transparent;
                        }

                        switch (tile.Model)
                        {
                            case BasicTile.BlockModel.Cross:
                                {
                                    AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, (float)Math.PI / 4 + (float)Math.PI), brightnessColor, tileTexture, tileState.FrontFace.Frame, SpriteEffects.FlipHorizontally, 1f, 0f);
                                    AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, -(float)Math.PI / 4 + (float)Math.PI), brightnessColor, tileTexture, tileState.FrontFace.Frame, SpriteEffects.FlipHorizontally, 1f, 0f);
                                    AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, (float)Math.PI / 4), brightnessColor, tileTexture, tileState.FrontFace.Frame, SpriteEffects.FlipHorizontally, 1f, 0f);
                                    AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, -(float)Math.PI / 4), brightnessColor, tileTexture, tileState.FrontFace.Frame, SpriteEffects.FlipHorizontally, 1f, 0f);
                                }
                                break;
                            case BasicTile.BlockModel.Extruded:
                                {

                                }
                                break;
                            default:
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
                                        AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI, 0), brightnessColor, tileTexture, tileState.BottomFace.Frame, SpriteEffects.FlipVertically);

                                    //front
                                    if (tileState.FrontFace.active)
                                        AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, 0), brightnessColor, tileTexture, tileState.FrontFace.Frame);

                                    //back
                                    if (tileState.BackFace.active)
                                        AddQuad(new Vector3(x, y, z), new Vector3((float)Math.PI / 2, (float)Math.PI / 2, -(float)Math.PI / 2), brightnessColor, tileTexture, tileState.BackFace.Frame, SpriteEffects.FlipHorizontally);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void AddQuad(Vector3 position, Vector3 ypr, Color color, Texture2D texture, Vector2 framePosition = default, SpriteEffects effects = SpriteEffects.None, float scale = 1f, float distFromCenter = 0.5f)
        {
            AddQuad(position, ypr, color, new Vector2(scale), texture, framePosition, effects, distFromCenter);
        }

        private void AddQuad(Vector3 position, Vector3 ypr, Color color, Vector2 scale, Texture2D texture, Vector2 framePosition = default, SpriteEffects effects = SpriteEffects.None, float distFromCenter = 0.5f)
        {
            AddQuad(position, ypr, color, scale, texture, new Rectangle((int)framePosition.X, (int)framePosition.Y, 16, 16), effects, distFromCenter);
        }

        private void AddQuad(Vector3 position, Vector3 ypr, Color color, Vector2 scale, Texture2D texture, Rectangle frame, SpriteEffects effects = SpriteEffects.None, float distFromCenter = 0.5f)
        {
            float xSize = 1f / texture.Width;
            float ySize = 1f / texture.Height;

            if (!TextureVertices.ContainsKey(texture))
                TextureVertices[texture] = new List<VertexPositionColorNormalTexture>();

            float xMin = frame.X * xSize;
            float xMax = xMin + (frame.Width * xSize);

            float yMin = frame.Y * ySize;
            float yMax = yMin + (frame.Height * ySize);

            if (effects.HasFlag(SpriteEffects.FlipHorizontally))
                (xMin, xMax) = (xMax, xMin);

            if (effects.HasFlag(SpriteEffects.FlipVertically))
                (yMin, yMax) = (yMax, yMin);

            Vector2 scaleSize = 0.5f * scale;

            VertexPositionColorNormalTexture vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(-scaleSize.X, distFromCenter, -scaleSize.Y), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMin, yMin));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(scaleSize.X, distFromCenter, -scaleSize.Y), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMax, yMin));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(-scaleSize.X, distFromCenter, scaleSize.Y), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMin, yMax));
            TextureVertices[texture].Add(vertex);


            vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(scaleSize.X, distFromCenter, -scaleSize.Y), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMax, yMin));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(scaleSize.X, distFromCenter, scaleSize.Y), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                color,
                new Vector2(xMax, yMax));
            TextureVertices[texture].Add(vertex);

            vertex = new VertexPositionColorNormalTexture(
                position + Vector3.Transform(new Vector3(-scaleSize.X, distFromCenter, scaleSize.Y), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
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

        private void AddWaterPlane(Vector3 position, float scale = 1f, Vector3 ypr = default, Color PlaneColor = default)
        {
            if(WaterBufferList == null)
                WaterBufferList = new List<VertexPositionColorTexture>();

            VertexPositionColorTexture vertex = new VertexPositionColorTexture(
                position + Vector3.Transform(new Vector3(0, 0, 0), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                PlaneColor,
                new Vector2(0, 0));
            WaterBufferList.Add(vertex);

            vertex = new VertexPositionColorTexture(
                position + Vector3.Transform(new Vector3(scale, 0, 0), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                PlaneColor,
                new Vector2(1, 0));
            WaterBufferList.Add(vertex);

            vertex = new VertexPositionColorTexture(
                position + Vector3.Transform(new Vector3(0, 0, scale), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                PlaneColor,
                new Vector2(0, 1));
            WaterBufferList.Add(vertex);




            vertex = new VertexPositionColorTexture(
                position + Vector3.Transform(new Vector3(scale, 0, 0), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                PlaneColor,
                new Vector2(1, 0));
            WaterBufferList.Add(vertex);

            vertex = new VertexPositionColorTexture(
                position + Vector3.Transform(new Vector3(scale, 0, scale), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                PlaneColor,
                 new Vector2(1, 1));
            WaterBufferList.Add(vertex);

            vertex = new VertexPositionColorTexture(
                position + Vector3.Transform(new Vector3(0, 0, scale), Quaternion.CreateFromYawPitchRoll(ypr.X, ypr.Y, ypr.Z)),
                PlaneColor,
                new Vector2(0, 1));
            WaterBufferList.Add(vertex);
        }
    }
}
