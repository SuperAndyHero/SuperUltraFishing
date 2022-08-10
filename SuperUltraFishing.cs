using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace SuperUltraFishing
{
	public class SuperUltraFishing : Mod
	{
        public override void Load()
        {
            ContentHandler.Load();
            //Model model = ContentHandler.GetModel("SuperUltraFishing/Models/20Dice");
        }

        public override void Unload()
        {
            ContentHandler.Unload();
        }
    }
}