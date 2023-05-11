using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SuperUltraFishing;
using System;
using Terraria.ModLoader;

public class FishBone : Entity3D
{
    public override string ModelPath => "SuperUltraFishing/Models/FishBone";
    public override string TexturePath => "SuperUltraFishing/Models/fishbone_alb";

    public override float BoundingSphereScale => 5f;
    public override float ModelScale => 0.01f;

    bool animate = false;
    int offset = 0;
    public override void OnCreate()
    {
        Scale = 1f;
        animate = true;
    }
    public override void Animate()
    {
        //if (animate)
        //{
            offset++;
            BoneTransforms[4] = Matrix.CreateFromYawPitchRoll(0, (float)Math.Sin((Terraria.Main.GameUpdateCount + offset) / 50f) / 10f, 0);
        //}
        //if (animate)
            //Yaw += 0.1f;
    }
    public override void PreUpdate()
    {
        const float SinkSpeed = 0.001f;
        //if (Velocity.Y < 0.01f)
        //   Velocity.Y -= SinkSpeed;
    }
    public override void AI()
    {
        //make this built into entities
        const float SlowDown = 0.942f;

        Velocity *= SlowDown;
    }
}