using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SuperUltraFishing.UI;
using System;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;
using static Terraria.ModLoader.PlayerDrawLayer;

namespace SuperUltraFishing.Items
{
	public class Sub : ModItem
	{
		public enum CaseType
		{
			DebugCasing,
			CasingName1,
			CasingName2,
			CasingName3,
			CasingName4
		}

		public CaseType CasingType { 
			get { 
				return _casingType; 
			} 
			set { 
				_casingType = value;
				CasingName = Enum.GetName(typeof(CaseType), value);
			} 
		}

        private CaseType _casingType;
        public string CasingName;

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

			CasingType = CaseType.DebugCasing;
        }

        public override bool? UseItem(Player player)
        {
			if (player.altFunctionUse == 2)
			{
				//ModContent.GetInstance<RobotUISystem>().ShowUI(player.Center);
				int caseint = (int)CasingType;
				caseint++;
				if (caseint > 4)
					caseint = 0;
				CasingType = (CaseType)caseint;
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
            Texture2D Base = Request<Texture2D>("SuperUltraFishing/Items/SubBase").Value;
            spriteBatch.Draw(Base, position, frame, drawColor, 0f, origin, scale, default, 0f);//drawColor.MultiplyRGBA(itemColor)

            Texture2D Casing = Request<Texture2D>("SuperUltraFishing/Items/" + CasingName).Value;
            spriteBatch.Draw(Casing, position, frame, drawColor, 0f, origin, scale, default, 0f);

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D Base = Request<Texture2D>("SuperUltraFishing/Items/SubBase").Value;
            spriteBatch.Draw(Base, Item.position - Main.screenPosition, null, lightColor * alphaColor.A, rotation, default, scale, default, 0f);

            Texture2D Casing = Request<Texture2D>("SuperUltraFishing/Items/" + CasingName).Value;
            spriteBatch.Draw(Casing, Item.position - Main.screenPosition, null, lightColor * alphaColor.A, rotation, default, scale, default, 0f);
            return false;
        }
    }
}