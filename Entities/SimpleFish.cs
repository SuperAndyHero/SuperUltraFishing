using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SuperUltraFishing;
using System;
using Terraria;
using Terraria.ModLoader;

public class SimpleFish : Entity3D
{
    public override string ModelPath => "SuperUltraFishing/Models/simplefish";
    public override Color ModelColor => new Color(ColorShuffleR(index, 0.5f), ColorShuffleG(index, 0.5f), ColorShuffleB(index, 0.5f));//unused
    public override float ModelScale => 0.004f;//only effects visual scale, does not effect bounding sphere (which is based on model size)
    public override float BoundingSphereScale => 5;

    public float ColorShuffleR(float x, float freq)
    {
        return (float)Math.Cos((x * freq * (float)Math.Tau) / 3);
    }

    public float ColorShuffleG(float x, float freq)
    {
        return (float)Math.Cos(((x * freq * (float)Math.Tau) / 3) + ((float)Math.PI / 1.5f));
    }

    public float ColorShuffleB(float x, float freq)
    {
        return (float)Math.Cos(((x * freq * (float)Math.Tau) / 3) - ((float)Math.PI / 1.5f));
    }

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
        Velocity = 0.1f * new Vector3(ColorShuffleR((index * 49) + ((float)Main.GameUpdateCount / 50f), 1.428f), ColorShuffleG((index * 53.3f) + ((float)Main.GameUpdateCount / 50f), 0.33f), ColorShuffleB((index * 45.55f) + ((float)Main.GameUpdateCount / 50f), 0.714f));
        const float SinkSpeed = 0.001f;
        if (Velocity.Y < 0.01f)
           Velocity.Y -= SinkSpeed;
    }
    public override void AI()
    {
        //make this built into entities
        const float SlowDown = 0.942f;

        Velocity *= SlowDown;
    }

    public override void Draw()
    {
        base.Draw();
    }
}