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

namespace SuperUltraFishing
{
	public static class ContentHandler
	{
		private static ConstructorInfo createContentReaderConstructor_Info;

		private static MethodInfo readAsset_Info;
		private static Func<ContentReader, object> readAsset;

		public static string extension;

		public static Dictionary<string, object> assetCache = new Dictionary<string, object>();

		public static void Load(string xnbExtension = ".xnc")//extension name option since tml refuses to compile mods with a model xnb file in them.
		{
			//This took a lot of poking around in both TML's and XNA's loading to get this to work
			//This was then narrowed down to the minimal reflection needed

			extension = xnbExtension;
            
			//We grab info about a internal method in the xna framework
			createContentReaderConstructor_Info = typeof(ContentReader).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(ContentManager), typeof(Stream), typeof(string), typeof(int), typeof(char), typeof(Action<IDisposable>) });
            
			readAsset_Info = typeof(ContentReader).GetMethod("ReadAsset", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(typeof(object));
			readAsset = (Func<ContentReader, object>)Delegate.CreateDelegate(typeof(Func<ContentReader, object>), readAsset_Info);

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
            
        }

		public static Model GetModel(string path) =>
			GetAsset<Model>(path);

		public static BasicEffect GetMaterial(string path) =>
			GetAsset<BasicEffect>(path);

		//Called this to match `ModContent.GetModTexture(str path)`
		public static Texture2D GetXnaTexture(string path) =>
			GetAsset<Texture2D>(path);

		//caches gotten assets
		public static T GetAsset<T>(string path)
		{
			if (assetCache.ContainsKey(path))
				return (T)assetCache[path];
			else
			{
				T asset = LoadAsset<T>(path + extension);
				assetCache.Add(path, asset);
				return asset;
			}
		}

		/// <summary>
		/// Reads asset from memory, Use GetAsset instead to get from cache if asset has already been read.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path">includes extension</param>
		/// <returns></returns>
		public static T LoadAsset<T>(string path) =>
			LoadAsset<T>(new MemoryStream(ModContent.GetFileBytes(path)));

		//stream.Seek(offset, SeekOrigin.Begin);
		//byte[] buffer = new byte[stream.Length];
		//stream.Read(buffer);
		//string buffer2 = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

		/// <summary>
		/// Reads asset from memory, Use GetAsset instead to get from cache if asset has already been read.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="stream"></param>
		/// <returns></returns>
		public static T LoadAsset<T>(Stream stream)
		{
			const int offset = 10;

            T asset = default;
            stream.Seek(offset, SeekOrigin.Begin); //skips first 10 bytes
			using ContentReader contentReader = (ContentReader)createContentReaderConstructor_Info.Invoke(new object[] { Main.ShaderContentManager, stream, "", 0, 'w', null });
			Main.QueueMainThreadAction(() => { //handles graphics so this must be queued on the main thread
                asset = (T)readAsset(contentReader); });
            return asset;
        }
	}
}