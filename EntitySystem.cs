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

namespace SuperUltraFishing
{
    public abstract class Entity3D
    {
        internal EntitySystem EntitySystem;

        public ushort index;
        public Vector3 Velocity = Vector3.Zero;
        public Vector3 Position = Vector3.Zero;
        public float Yaw = 0;
        public float Pitch = 0;
        public float Scale = 1f;
        public bool active = true;
        public Model Model { get; private set; }
        public Asset<Texture2D> Texture { get; private set; }
        public Matrix[] BoneTransforms;
        public BoundingSphere BoundingSphere;//may need to be a array

        public Entity3D() 
        {
            EntitySystem = GetInstance<EntitySystem>();
            Model = ContentHandler.GetAsset<Model>(ModelPath);
            Texture = Request<Texture2D>(TexturePath);
            BoneTransforms = Enumerable.Repeat(Matrix.Identity, Model.Bones.Count).ToArray();
            BoundingSphere = Model.Meshes[0].BoundingSphere;
            SetEffect();
            OnCreate();
        }

        public virtual void SetEffect()
        {
            foreach (var mesh in Model.Meshes)
                foreach (var meshpart in mesh.MeshParts)
                {
                    meshpart.Effect = EntitySystem.rendering.ModelEffect;//.Clone();//may want to clone this or have a instance per model
                }
        }

        public virtual void OnCreate() { }

        //public virtual string DisplayName => "";
        public virtual float MoveSpeed => 1f;
        public virtual string ModelPath => "SuperUltraFishing/Models/20Dice";
        public virtual string TexturePath => null;

        //onetimecreate
        public void Update()
        {
            //Velocity = Vector3.One * (float)Math.Sin((Main.GameUpdateCount) / 25f) * 0.1f;
            PreUpdate();

            //might be worth optimizing using Vector3.Add(ref pos, ref vel, out pos)
            Position += Velocity;

            //sets bounding sphere to position
            Matrix SphereTransform = Matrix.CreateScale(Scale) * Matrix.CreateTranslation(Position);//may need rotation
            BoundingSphere = Model.Meshes[0].BoundingSphere.Transform(SphereTransform);

            //collision
            if (BoundingSphere.Intersects(EntitySystem.player.BoundingSphere))
            {
                //buggy collision solving
                //Main.NewText("Colliding");
                Vector3 dirVector = Vector3.Normalize(BoundingSphere.Center - EntitySystem.player.BoundingSphere.Center);

                //float playerVelLength = EntitySystem.player.Velocity.Length();
                //float velLength = Velocity.Length();

                //float ratio = playerVelLength == 0 ? 1 : velLength == 0 ? 1 : playerVelLength / velLength;
                //float ratio2 = playerVelLength == 0 ? 1 : velLength == 0 ? 1 : velLength / playerVelLength;

                //Position += (vector * EntitySystem.player.Velocity.Length() * 1f);// * 2;
                //EntitySystem.player.Position -= (vector * Velocity.Length() * 1f);// * 2;

                Velocity += (dirVector * EntitySystem.player.Velocity.Length() * 0.5f);// * 2;
                EntitySystem.player.Velocity -= (dirVector * Velocity.Length() * 0.5f);// * 2;

                //Velocity /= ratio2;
                //EntitySystem.player.Velocity /= ratio;
            }

            AI();
            Animate();
        }
        public virtual void PreUpdate() { }
        public virtual void AI() { }
        public virtual void Animate() { }


        public virtual void Draw()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int tilePosX = (int)((BoundingSphere.Center.X / 10f) - 0.5f) + i;
                        int tilePosY = (int)((BoundingSphere.Center.Y / 10f) - 0.5f) + j;
                        int tilePosZ = (int)((BoundingSphere.Center.Z / 10f) - 0.5f) + k;

                        if (EntitySystem.world.ValidTilePos(tilePosX, tilePosY, tilePosZ) && EntitySystem.world.TempCollisionType(tilePosX, tilePosY, tilePosZ) == 1)
                        {
                            Matrix ScalePosBounds = Matrix.CreateScale(1.001f) * Matrix.CreateTranslation(
                                    new Vector3(
                                    tilePosX * 10,
                                    tilePosY * 10,
                                    tilePosZ * 10));
                            EntitySystem.rendering.DebugCube.Draw(EntitySystem.rendering.WorldMatrix * ScalePosBounds, EntitySystem.rendering.ViewMatrix, EntitySystem.rendering.ProjectionMatrix);
                        }
                    }
                }
            }

            if (Model != null)
            {
                //this assumes the model has all parts set to the default effect, this may need to be changed to use a cached effect reference, or change the values for each mech part seperately
                SkinnedEffect effect = EntitySystem.rendering.ModelEffect;
                effect.Texture = Texture.Value;
                effect.SetBoneTransforms(BoneTransforms);

                Matrix ScaleRotPos = Matrix.CreateScale(Scale) * Matrix.CreateFromYawPitchRoll(Yaw, Pitch, 0) * Matrix.CreateTranslation(Position);
                Model.Draw(EntitySystem.rendering.WorldMatrix * ScaleRotPos, EntitySystem.rendering.ViewMatrix, EntitySystem.rendering.ProjectionMatrix);

                if(true)//debug
                {
                    Matrix ScalePosBounds = Matrix.CreateScale(BoundingSphere.Radius) * Matrix.CreateTranslation(BoundingSphere.Center);
                    EntitySystem.rendering.DebugSphere.Draw(EntitySystem.rendering.WorldMatrix * ScalePosBounds, EntitySystem.rendering.ViewMatrix, EntitySystem.rendering.ProjectionMatrix);
                }
            }

            PostDraw();
        }
        public virtual void PostDraw() { }
    }

    internal class EntitySystem : ModSystem
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