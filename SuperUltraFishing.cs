using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using static Terraria.ModLoader.Core.TmodFile;

namespace SuperUltraFishing
{
	public class SuperUltraFishing : Mod
	{
        public override void Load()
        {
            ContentHandler.Load();
            //Model fish = ContentHandler.GetAsset<Model>("SuperUltraFishing/Models/FishBone").Result;

            //string name = "FlatColor";
            //var screenRef = new Ref<Effect>(Assets.Request<Effect>("Effects/FlatColor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value);
            //Filters.Scene[name] = new Filter(new ScreenShaderData(screenRef, name + "Pass"), EffectPriority.High);
            //Filters.Scene[name].Load();
        }

        public override void Unload()
        {
            ContentHandler.Unload();
        }

        public static (int x, int y, int z) TileSidesOffset(int loop)
        {
            switch (loop)
            {
                default:
                    return (0, 1, 0);
                case 1:
                    return (1, 0, 0);
                case 2:
                    return (0, 0, 1);
                case 3:
                    return (-1, 0, 0);
                case 4:
                    return (0, 0, -1);
                case 5:
                    return (0, -1, 0);
            }
        }
    }
}