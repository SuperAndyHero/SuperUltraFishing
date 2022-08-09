using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using static Terraria.ModLoader.ModContent;

namespace SuperUltraFishing
{
    public class ChaosEditionSystem : ModSystem
    {
        //updates and removes codes, also selects codes if timer is up
        public override void PostUpdateInput()
        {
           
        }

        //hook for codes that modify interface layers
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            
        }

        //countdown timer and debug active codes list
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            
        }
    }
}