using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SuperUltraFishing;
using System;
using Terraria;
using Terraria.ModLoader;

public class Chest : Entity3D
{
    public override string ModelPath => "SuperUltraFishing/Models/Chest";
    public override float ModelScale => 0.004f;//only effects visual scale, does not effect bounding sphere (which is based on model size)
    public override float BoundingSphereScale => 5;

    public override string TexturePath => "SuperUltraFishing/Models/SimplefishUV";
    public override void OnCreate()
    {
        Scale = 0.3f;
    }
    public override void Animate()
    {
        Vector2 HorizontalVel = new Vector2(Velocity.X, -Velocity.Z);
        Yaw = HorizontalVel.ToRotation();
        //Yaw += 0.1f;
    }
    public override void PreUpdate()
    {
        
    }
    public override void AI()
    {
        Velocity = Vector3.Zero;
    }

    public override void Draw()
    {
        base.Draw();
    }
}