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
			DisplayName.SetDefault("SubName");
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

        //public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        //{

        //	return true;
        //      }
    }
}