using Terraria.ModLoader;
using System;
using Terraria;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Terraria.ModLoader.IO;
using ReLogic.Content;
using System.Threading.Tasks;

namespace SuperUltraFishing
{
    public class ModContentManager : ContentManager
    {
        public ModContentManager(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override Stream OpenStream(string assetName)
        {
            return new MemoryStream(ModContent.GetFileBytes(assetName));
        }
    }


    public static class ContentHandler
    {
        public static string extension;
        public static ModContentManager modContentManager;

        public static void Load(string xnbExtension = ".xnb")//extension name option since tml refuses to compile mods with a model xnb file in them.
        {
            //This took a lot of poking around in both TML's and XNA's loading to get this to work
            //This was then narrowed down to the minimal reflection needed

            extension = xnbExtension;
            modContentManager = new ModContentManager(Main.instance.Content.ServiceProvider);

            //quick info
            //MemoryStream streams from memory
            //FileStream streams from disk
            //Both extend from stream
            //Here we are swapping out a filestream for a memory stream

            //model type is 'Model' material type is 'BasicEffect'
            //modelSans = LoadAsset<Model>("Realms/Models/Sans.xnc");
            //modelSans.SetTexture(ModContent.GetTexture("Realms/Models/Sans Tex"));

            //modelRock = LoadAsset<Model>("Realms/Models/LargeAsteroid.xnc");
            //modelRock.SetTexture(ModContent.GetTexture("Realms/Models/AsteroidTexture"));

            //modelSphere = LoadAsset<Model>("Realms/Models/UntexturedSphere.xnc");
        }

        public static void Unload()
        {
            extension = null;
            modContentManager = null;
        }

        public static T GetAsset<T>(string path)
        {
            if(AssetRepository.IsMainThread)
                return modContentManager.Load<T>(path + ".xnb");
            else
                return Main.RunOnMainThread<T>(() =>
                {
                    return modContentManager.Load<T>(path + ".xnb");
                }).Result;
        }

        public static Model GetModel(string path) =>
            GetAsset<Model>(path);

        public static BasicEffect GetMaterial(string path) =>
            GetAsset<BasicEffect>(path);

        //Called this to match `ModContent.GetModTexture(str path)`
        public static Texture2D GetXnaTexture(string path) =>
            GetAsset<Texture2D>(path);


    }
}