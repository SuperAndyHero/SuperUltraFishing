using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using static Terraria.ModLoader.PlayerDrawLayer;

namespace SuperUltraFishing.Render
{
    public class UIRendering
    {
        private World world;
        private RobotPlayer player;
        private Rendering rendering;

        public UIRendering(Rendering rendering)
        {
            this.rendering = rendering;
        }

        public void PostLoad(World world, RobotPlayer player)
        {
            this.world = world;
            this.player = player;
        }

        public void DrawUI()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            Texture2D Hud = ModContent.Request<Texture2D>("SuperUltraFishing/Items/SubBase").Value;
            //Main.spriteBatch.Draw(Hud,)
            Main.spriteBatch.End();
        }
    }
}
