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
using SuperUltraFishing.World;

namespace SuperUltraFishing.Render
{
    public class Rendering : ModSystem
    {
        private GameWorld world;
        private RobotPlayer player;
        private EntitySystem entitySystem;
        private FishingUIWindow fishingUIWindow;

        public UIRendering UI;
        public MeshRendering Mesh;
        public BiomeRendering Biome;


        public RenderTarget2D WindowTarget;//Main drawing
        public RenderTarget2D WaterTarget;//Target for  water distortion map

        public Effect WaterPostProcessEffect;//Distortion pass (pixel shader)

        public Texture2D LargePerlin;//Perlin maps for water
        public Texture2D SmallPerlin;

        public override void Load()
        {
            Main.QueueMainThreadAction(() =>
            {
                if (!Main.dedServ)
                {
                    Mesh = new MeshRendering(this);
                    UI = new UIRendering(this);
                    Biome = new BiomeRendering(this);

                    SmallPerlin = Request<Texture2D>("SuperUltraFishing/Effects/SmallPerlin", AssetRequestMode.ImmediateLoad).Value;
                    LargePerlin = Request<Texture2D>("SuperUltraFishing/Effects/LargePerlin", AssetRequestMode.ImmediateLoad).Value;

                    WindowTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, default, DepthFormat.Depth24Stencil8);
                    WaterTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, SurfaceFormat.Rg32, DepthFormat.Depth24Stencil8);


                    WaterPostProcessEffect = Mod.Assets.Request<Effect>("Effects/WaterPostProcess", AssetRequestMode.ImmediateLoad).Value;
                }
            });
        }

        public override void PostAddRecipes()
        {
            world = GetInstance<GameWorld>();
            player = GetInstance<RobotPlayer>();
            entitySystem = GetInstance<EntitySystem>();
            fishingUIWindow = GetInstance<FishingUIWindow>();

            if (!Main.dedServ)
            {
                Mesh.PostLoad(world, player);
                UI.PostLoad(world, player);
            }
        }

        public override void PostDrawTiles()
        {
            if (fishingUIWindow.WindowActive)
            {
                Mesh.Draw();

                entitySystem.DrawEntities();
                player.DrawPlayer();

                Mesh.DrawWater();
                //anything drawn in tis will be effect by the water distort shader, since it is applied when the render target is drawn to the screen
                Main.graphics.GraphicsDevice.SetRenderTarget(null);
            }
        }
    }
}
