using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SuperUltraFishing;
using System;
using Terraria.ModLoader;

public class FishBone : Entity3D
{
    public override string ModelPath => "SuperUltraFishing/Models/FishBone";
    public override string TexturePath => "SuperUltraFishing/Models/fishbone_alb";
    bool animate = false;
    int offset = 0;
    public override void OnCreate()
    {
        Scale = 0.02f;
        animate = false;
    }
    public override void Animate()
    {
        //if (animate)
        //{
            offset++;
            BoneTransforms[4] = Matrix.CreateFromYawPitchRoll(0, (float)Math.Sin((Terraria.Main.GameUpdateCount + offset) / 50f) / 10f, 0);
            //Model.Bones[4].Transform = Matrix.CreateFromYawPitchRoll((float)Math.Sin(Terraria.Main.GameUpdateCount / 50f), 0, 0);
        //}
        if (animate)
            Yaw += 0.01f;
    }
    public override void AI()
    {

    }
}