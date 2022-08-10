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
    public class FishingModSystem : ModSystem
    {
        public BasicTile[,,] AreaArray = new BasicTile[16, 16, 16];

        public RenderTarget2D WindowTarget;

        public Vector3 CameraPosition = Vector3.Zero;
        public float CameraYaw = 0;
        public float CameraPitch = 0;

        public static BasicEffect basicEffect;
        
        public override void Load()
        {
            //int sizeX = AreaArray.GetLength(0);
            //int sizeY = AreaArray.GetLength(1);
            //int sizeZ = AreaArray.GetLength(2);
            //for (int x = 0; x < sizeX; x++)
            //{
            //    for (int y = 0; y < sizeY; y++)
            //    {
            //        for (int z = 0; z < sizeZ; z++)
            //        {
            //            AreaArray[x, y, z] = new BasicTile();
            //        }
            //    }
            //}

            PlaceTile(TileID.Grass, 8, 8, 8);
            PlaceTile(TileID.Grass, 8, 9, 8);
            PlaceTile(TileID.Stone, 8, 8, 7);
            PlaceTile(TileID.Stone, 8, 9, 7);

            PlaceTile(TileID.Stone, 2, 2, 2);
            PlaceTile(TileID.Stone, 2, 3, 2);
            PlaceTile(TileID.Stone, 2, 4, 2);
            PlaceTile(TileID.Stone, 2, 5, 2);
            PlaceTile(TileID.Stone, 0, 0, 0);
            PlaceTile(TileID.Stone, 15, 15, 15);

            Main.QueueMainThreadAction(() =>
            {
                if (!Main.dedServ)
                {
                    WindowTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, default, DepthFormat.Depth24Stencil8);
                    //WindowTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, 224, 320);
                    basicEffect = new BasicEffect(Main.graphics.GraphicsDevice)
                    {
                        VertexColorEnabled = true,
                        TextureEnabled = true,
                        Texture = Terraria.GameContent.TextureAssets.BlackTile.Value
                    };
                    basicEffect.Projection = Matrix.CreateOrthographic(Main.screenWidth, Main.screenHeight, 0, 1000);
                }
            });
        }

        public void PlaceTile(ushort type, int x, int y, int z)
        {
            AreaArray[x, y, z].TileType = type;
            AreaArray[x, y, z].Active = true;
        }


        public List<VertexPositionColorTexture> TileMeshVertices = new();
        public VertexBuffer VertBuffer;
        bool istrue = false;
        public void BuildTileMesh()
        {
            //Main.NewText(AreaArray[8, 8, 8].TileType);
            //Main.NewText(AreaArray[8, 8, 8].HasTile);
            TileMeshVertices.Clear();

            int sizeX = AreaArray.GetLength(0);
            int sizeY = AreaArray.GetLength(1);
            int sizeZ = AreaArray.GetLength(2);

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        BasicTile tile = AreaArray[x, y, z];
                        if (!tile.Active)
                            continue;

                        float colorMult = 1f;// new Vector3(x, y, z).Length() / new Vector3(sizeX, sizeY, sizeZ).Length();

                        if (!(x + 1 < sizeZ) || !AreaArray[x + 1, y, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, 0, -(float)Math.PI / 2), Color.Purple * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);

                        if (!(x - 1 >= 0) || !AreaArray[x - 1, y, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, 0, (float)Math.PI / 2), Color.Green * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);

                        if (!(y + 1 < sizeZ) || !AreaArray[x, y + 1, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, 0, 0), Color.White * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);


                        if (!(y - 1 >= 0) || !AreaArray[x, y - 1, z].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI, 0), Color.Red * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);

                        if (!(z + 1 < sizeZ) || !AreaArray[x, y, z + 1].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, (float)Math.PI / 2, 0), Color.Yellow * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);

                        if (!(z - 1 >= 0) || !AreaArray[x, y, z - 1].Active)
                            AddQuad(new Vector3(x, y, z), new Vector3(0, -(float)Math.PI / 2, 0), Color.Blue * colorMult, Terraria.GameContent.TextureAssets.BlackTile.Value);
                    }
                }
            }

            AddFloorPlane();

            VertBuffer = new VertexBuffer(Main.graphics.GraphicsDevice, typeof(VertexPositionColorTexture), TileMeshVertices.Count + 1, BufferUsage.WriteOnly);
            VertBuffer.SetData(TileMeshVertices.ToArray());
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

        public void GenerateWorld()
        {
            int sizeX = AreaArray.GetLength(0);
            int sizeY = AreaArray.GetLength(1);
            int sizeZ = AreaArray.GetLength(2);
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        //if(x == 0 || y == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1)
                        //    AreaArray[x, y, z].Active = true;
                        //else
                        //    AreaArray[x, y, z].Active = false;
                        AreaArray[x, y, z].Active = (x % 5 == 0 || y % 3 == 0 || z % 7 == 0);
                        AreaArray[x, y, z].Active = false;
                    }
                }
            }
        }

        //updates and removes codes, also selects codes if timer is up
        public int lastMouseX = 0;
        public int lastMouseY = 0;
        public override void PostUpdateInput()
        {
            if (!Main.gameMenu)
            {
                //GenerateWorld();

                //BuildTileMesh();

                Main.cursorScale = 0;
                basicEffect.Texture = Terraria.GameContent.TextureAssets.Ninja.Value;

                if ((new Vector2(Main.mouseX, Main.mouseY) - new Vector2(Main.screenWidth / 2, Main.screenHeight / 2)).Length() > 200)
                {
                    int mouseXdiff = Main.mouseX - lastMouseX;
                    int mouseYdiff = Main.mouseY - lastMouseY;
                    Mouse.SetPosition(Main.screenWidth / 2, Main.screenHeight / 2);
                    Main.mouseX = Main.screenWidth / 2;
                    Main.mouseY = Main.screenHeight / 2;
                    lastMouseX = Main.mouseX - mouseXdiff;
                    lastMouseY = Main.mouseY - mouseYdiff;
                }

                CameraYaw -= (Main.mouseX - lastMouseX) * 0.001f;
                CameraPitch -= (Main.mouseY - lastMouseY) * 0.003f;

                if (Main.keyState.IsKeyDown(Keys.Down))
                    CameraPitch -= 0.015f;
                if (Main.keyState.IsKeyDown(Keys.Up))
                    CameraPitch += 0.015f;
                if (Main.keyState.IsKeyDown(Keys.Left))
                    CameraYaw += 0.015f;
                if (Main.keyState.IsKeyDown(Keys.Right))
                    CameraYaw -= 0.015f;

                Vector3 newDir = Vector3.Zero;
                if (Main.keyState.IsKeyDown(Keys.S))
                    newDir.Z += 1;
                if (Main.keyState.IsKeyDown(Keys.W))
                    newDir.Z -= 1;
                if (Main.keyState.IsKeyDown(Keys.A))
                    newDir.X -= 1;
                if (Main.keyState.IsKeyDown(Keys.D))
                    newDir.X += 1;
                CameraPosition += Vector3.Transform(newDir, Matrix.CreateFromYawPitchRoll(CameraYaw, CameraPitch, 0));

                lastMouseX = Main.mouseX;
                lastMouseY = Main.mouseY;
            }
        }

        //hook for codes that modify interface layers
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            Main.mouseText = true;
            Main.signHover = -1;
            Main.player[Main.myPlayer].mouseInterface = true;
        }

        //countdown timer and debug active codes list
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(WindowTarget, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);
        }

        public override void PostDrawTiles()
        {
            Main.graphics.GraphicsDevice.SetRenderTarget(WindowTarget);
            Main.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            if (VertBuffer != null)
            {
                basicEffect.View = Matrix.CreateLookAt(CameraPosition, CameraPosition + Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(CameraYaw, CameraPitch, 0)), Vector3.Up);
                //Main.NewText("yaw: " + CameraYaw);
                //Main.NewText("pitch: " + CameraPitch);
                basicEffect.World = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up) * Matrix.CreateScale(10);// Matrix.CreateFromYawPitchRoll(CameraYaw, CameraPitch, 0)
                basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 2f, (float)Main.screenWidth / (float)Main.screenHeight, 1, 2000);

                basicEffect.CurrentTechnique.Passes[0].Apply();
                Main.graphics.GraphicsDevice.SetVertexBuffer(VertBuffer);
                Main.graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, VertBuffer.VertexCount / 3);
            }
            Main.graphics.GraphicsDevice.SetRenderTarget(null);
        }
    }
}