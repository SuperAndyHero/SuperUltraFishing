using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace SuperUltraFishing
{
	public class SuperUltraFishing : Mod
	{
		public Tile[,,] AreaArray;

		public RenderTarget2D WindowTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, 224, 320);
	}
}