using System;
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
using System.Configuration;
using ReLogic.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Design;
using SuperUltraFishing.Render;
using static SuperUltraFishing.Collision;

namespace SuperUltraFishing
{
    public class EntitySystem : ModSystem
    {
        public World world;
        public Rendering rendering;
        public RobotPlayer player;

        public Entity3D[] EntityArray;
        const int MaxEntityCount = 100;

        public override void Load()
        {
            ClearAllEntities();
        }

        public override void PostAddRecipes()
        {
            world = GetInstance<World>();
            rendering = GetInstance<Rendering>();
            player = GetInstance<RobotPlayer>();
        }

        public override void PostUpdateNPCs()//may need a different hook
        {
            foreach(Entity3D entity in EntityArray)
            {
                entity?.Update();
            }
        }

        public void DrawEntities()
        {//These get drawn before the water pass
            foreach (Entity3D entity in EntityArray)
            {
                entity?.Draw();
            }
        }

        public void ClearAllEntities()
        {
            EntityArray = new Entity3D[MaxEntityCount];
        }

        public Entity3D SpawnEntity(Type type, Vector3 position)
        {
            if (type.IsSubclassOf(typeof(Entity3D)))
            {
                for (ushort i = 0; i < MaxEntityCount; i++)
                {
                    if (EntityArray[i] == null)
                    {
                        EntityArray[i] = (Entity3D)Activator.CreateInstance(type);
                        EntityArray[i].index = i;
                        EntityArray[i].Position = position;
                        Main.NewText("Spawned entity: " + type.ToString());
                        return EntityArray[i];
                    }
                }
                Main.NewText("Entity Array Full");
                return null;
            }
            Main.NewText("Type is not Entity3D");
            return null;
        }
    }

}