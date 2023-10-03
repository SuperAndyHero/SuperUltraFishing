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
using static SuperUltraFishing.Collision;

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
        public BoundingSphere TransformedBoundingSphere;//may need to be a array for having hitboxes be model part

        public virtual float ModelScale => 1f;
        public virtual float BoundingSphereScale => 1f;//rename to size
        public virtual Color ModelColor => Color.White;

        public Entity3D()
        {
            EntitySystem = GetInstance<EntitySystem>();
            Model = ContentHandler.GetAsset<Model>(ModelPath);
            if(TexturePath != null)
                Texture = Request<Texture2D>(TexturePath);
            BoneTransforms = Enumerable.Repeat(Matrix.Identity, Model.Bones.Count).ToArray();
            TransformedBoundingSphere = new BoundingSphere(default, 1);
            SetEffect();
            OnCreate();
        }

        public virtual void SetEffect()
        {
            foreach (var mesh in Model.Meshes)
                foreach (var meshpart in mesh.MeshParts)
                {
                    meshpart.Effect = EntitySystem.rendering.Mesh.ModelEffect;//.Clone();//may want to clone this or have a instance per model type
                }
        }

        public virtual void OnCreate() { }

        //public virtual string DisplayName => "";
        public virtual float MoveSpeed => 1f;
        public virtual string ModelPath => "SuperUltraFishing/Models/20Dice";
        public virtual string TexturePath => null;

        //public virtual Vector3 BoundingSphereOffset = Vector3.Zero;

        //onetimecreate
        public void Update()
        {
            //Velocity = Vector3.One * (float)Math.Sin((Main.GameUpdateCount) / 25f) * 0.1f;
            PreUpdate();

            //might be worth optimizing using Vector3.Add(ref pos, ref vel, out pos)
            Position += Velocity;

            //sets bounding sphere to position
            TransformedBoundingSphere = new BoundingSphere(Position, Scale * BoundingSphereScale);

            Collision();

            AI();
            Animate();
        }

        public void Collision()
        {
            //entity-player collision
            if (!EntitySystem.player.NoClip && TransformedBoundingSphere.Intersects(EntitySystem.player.BoundingSphere))
            {
                Vector3 collideOffset = CollideSphrWithSphr(TransformedBoundingSphere.Center, TransformedBoundingSphere.Radius, EntitySystem.player.BoundingSphere.Center, EntitySystem.player.BoundingSphere.Radius);

                Position += collideOffset;
                Velocity += (collideOffset * 0.05f);
                EntitySystem.player.Position -= collideOffset;
            }

            //entity-entity collision
            foreach (var ent in EntitySystem.EntityArray)
            {
                if (ent == null || ent == this)
                    continue;

                if (TransformedBoundingSphere.Intersects(ent.TransformedBoundingSphere))
                {
                    Vector3 collideOffset = CollideSphrWithSphr(TransformedBoundingSphere.Center, TransformedBoundingSphere.Radius, ent.TransformedBoundingSphere.Center, ent.TransformedBoundingSphere.Radius);

                    Position += collideOffset;
                    ent.Position -= collideOffset;
                }
            }

            //entity-tile collision
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 NewSphereCenter = CollideSphereWithTile(TransformedBoundingSphere, i, j, k, EntitySystem.world, out bool Collided);
                        Position -= TransformedBoundingSphere.Center - NewSphereCenter;
                        TransformedBoundingSphere.Center = NewSphereCenter;
                        //if (Collided)
                        //    Velocity *= 0.9f;
                    }
        }

        public virtual void PreUpdate() { }
        public virtual void AI() { }
        public virtual void Animate() { }


        public virtual void Draw()
        {
            //collision debug
            //for (int i = 0; i < 3; i++)
            //    for (int j = 0; j < 3; j++)
            //        for (int k = 0; k < 3; k++)
            //        {
            //            int tilePosX = (int)((TransformedBoundingSphere.Center.X / 10f) - 0.5f) + i;
            //            int tilePosY = (int)((TransformedBoundingSphere.Center.Y / 10f) - 0.5f) + j;
            //            int tilePosZ = (int)((TransformedBoundingSphere.Center.Z / 10f) - 0.5f) + k;

            //            if (EntitySystem.world.ValidTilePos(tilePosX, tilePosY, tilePosZ) && EntitySystem.world.TempCollisionType(tilePosX, tilePosY, tilePosZ) == 1)
            //            {
            //                Matrix ScalePosBounds = Matrix.CreateScale(1.001f) * Matrix.CreateTranslation(
            //                        new Vector3(
            //                        tilePosX * 10,
            //                        tilePosY * 10,
            //                        tilePosZ * 10));
            //                EntitySystem.rendering.DebugCube.Draw(EntitySystem.rendering.WorldMatrix * ScalePosBounds, EntitySystem.rendering.ViewMatrix, EntitySystem.rendering.ProjectionMatrix);
            //            }
            //        }

            if (Model != null)
            {
                //this assumes the model has all parts set to the default effect, this may need to be changed to use a cached effect reference, or change the values for each mech part seperately
                SkinnedEffect effect = EntitySystem.rendering.Mesh.ModelEffect;
                if(Texture != null)
                    effect.Texture = Texture.Value;
                effect.SetBoneTransforms(BoneTransforms);
                effect.DiffuseColor = ModelColor.ToVector3();

                Matrix ScaleRotPos = Matrix.CreateScale(Scale * ModelScale) * Matrix.CreateFromYawPitchRoll(Yaw, Pitch, 0) * Matrix.CreateTranslation(Position);
                Model.Draw(EntitySystem.rendering.Mesh.WorldMatrix * ScaleRotPos, EntitySystem.rendering.Mesh.ViewMatrix, EntitySystem.rendering.Mesh.ProjectionMatrix);
                //crashes if blendweight / blend index array does not exist in vertex dec since this uses skinned effect
                //having no vertex colors is fine
                //needs another effect if there needs to be a entity with no armature

                if (false)//debug sphere drawing
                {
                    Matrix ScalePosBounds = Matrix.CreateScale(TransformedBoundingSphere.Radius * 0.1f) * Matrix.CreateTranslation(TransformedBoundingSphere.Center);
                    EntitySystem.rendering.Mesh.DebugSphere.Draw(EntitySystem.rendering.Mesh.WorldMatrix * ScalePosBounds, EntitySystem.rendering.Mesh.ViewMatrix, EntitySystem.rendering.Mesh.ProjectionMatrix);
                }
            }

            PostDraw();
        }
        public virtual void PostDraw() { }
    }
}
