using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace SuperUltraFishing.Items
{
	public class DebugRod : ModItem
	{
		public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("DebugRod"); // By default, capitalization in classnames will add spaces to the display name. You can customize the display name here by uncommenting this line.
			//Tooltip.SetDefault("This is a basic modded sword.");
		}

		public override void SetDefaults()
		{
			Item.damage = 50;
			Item.DamageType = DamageClass.Melee;
			Item.width = 40;
			Item.height = 40;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = 1;
			Item.knockBack = 6;
			Item.value = 10000;
			Item.rare = 2;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = true;
			Item.createTile = ModContent.TileType<Debug>();
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.DirtBlock, 10);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}

        public override bool? UseItem(Player player)
        {
			GetInstance<FishingUIWindow>().ActivateWindow((Main.MouseWorld / 16).ToPoint16());

			return true;
        }

		public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
		{
			//Main.graphics.GraphicsDevice.RasterizerState.FillMode = FillMode.Solid;
			return true;
        }
	}

	public class Debug : ModTile
    {
		public override void SetStaticDefaults()
		{
			Main.tileShine[Type] = 1100;
			Main.tileSolid[Type] = true;
			Main.tileSolidTop[Type] = true;
			Main.tileFrameImportant[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.addTile(Type);
		}

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
			//Main.instance.TilesRenderer.AddSpecialLegacyPoint(new Point(i, j));
			//Main.graphics.GraphicsDevice.RasterizerState.FillMode = FillMode.Solid;
			base.DrawEffects(i, j, spriteBatch, ref drawData);
        }
        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
			//Main.graphics.GraphicsDevice.RasterizerState.FillMode = FillMode.WireFrame;
		}
    }
}