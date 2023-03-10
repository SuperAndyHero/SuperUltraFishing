using Microsoft.Xna.Framework.Graphics;
using SuperUltraFishing;
using Terraria.ModLoader;

public class FishBone : Entity3D
{
    public override string ModelPath => "SuperUltraFishing/Models/FishBone";
    public override void OnCreate()
    {
        Scale = 0.02f;
        Model.Meshes[0].MeshParts[0].Effect = ModContent.GetInstance<Rendering>().BasicEffect.Clone();
        ((BasicEffect)Model.Meshes[0].MeshParts[0].Effect).Texture = ModContent.Request<Texture2D>("SuperUltraFishing/Models/fishbone_alb", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
    }
    public override void Animate()
    {
        Yaw += 0.01f;
    }
    public override void AI()
    {

    }
}