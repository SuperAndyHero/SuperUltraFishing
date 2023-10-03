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
    public class UIRendering
    {
        private GameWorld world;
        private RobotPlayer player;
        private Rendering rendering;

        public UIRendering(Rendering rendering)
        {
            this.rendering = rendering;
        }

        public void PostLoad(GameWorld world, RobotPlayer player)
        {
            this.world = world;
            this.player = player;
        }

        //this is called from the window
        public void DrawUI(SpriteBatch sb, Rectangle windowSize)
        {
            //Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            Texture2D Hud = ModContent.Request<Texture2D>("SuperUltraFishing/UI/Sub_Hud").Value;
            float HudPosX = (windowSize.X + windowSize.Width * 0.75f);
            float HudPosY = (windowSize.Y + windowSize.Height);
            sb.Draw(Hud, new Vector2(HudPosX, HudPosY - Hud.Height), Color.White);
            Texture2D overlay = ModContent.Request<Texture2D>("SuperUltraFishing/UI/ChargedOverlay").Value;
            sb.Draw(overlay, new Vector2(HudPosX + 142, (HudPosY - overlay.Height) - 3), Color.White);


            Texture2D Crosshair = ModContent.Request<Texture2D>("SuperUltraFishing/UI/Crosshair").Value;
            sb.Draw(Crosshair, new Vector2((windowSize.X + (windowSize.Width / 2)) - Crosshair.Width / 2, (windowSize.Y + (windowSize.Height / 2)) - Crosshair.Height / 2), Color.White);
            //Main.spriteBatch.End();
        }
    }
}
