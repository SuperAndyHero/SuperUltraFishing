using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SuperUltraFishing.UI;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace SuperUltraFishing.Items
{
	public class Sub : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("SubNameHere");
			Tooltip.SetDefault("This is a basic modded sword.");
		}

		public override void SetDefaults()
		{
			Item.width = 48;
			Item.height = 38;

			Item.value = 10000;
			Item.rare = ItemRarityID.Blue;

			Item.useTime = 20;//make longer later to avoid item usage issues
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.HiddenAnimation;
			Item.autoReuse = false;
			Item.UseSound = SoundID.Item1;
		}

        public override bool? UseItem(Player player)
        {
			if (player.altFunctionUse == 2)
			{
				ModContent.GetInstance<RobotUISystem>().ShowUI(player.Center);
            }
			else
			{
				Vector2 pos = (Main.MouseWorld / 16);
				if (Vector2.Distance((player.Center / 16), pos) < 31)
				{
					Main.RunOnMainThread(() => GetInstance<FishingUIWindow>().ActivateWindow(pos.ToPoint16()));
                }
			}

			return true;
        }

		public override bool AltFunctionUse(Player player) => true;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D Base = ModContent.Request<Texture2D>("SuperUltraFishing/Items/SubBase").Value;
            spriteBatch.Draw(Base, position, frame, drawColor, 0f, origin, scale, default, 0f);//drawColor.MultiplyRGBA(itemColor)

            Texture2D Casing = ModContent.Request<Texture2D>("SuperUltraFishing/Items/BasicCasing").Value;
            spriteBatch.Draw(Casing, position, frame, drawColor, 0f, origin, scale, default, 0f);

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
			//todo
            return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }
    }
}