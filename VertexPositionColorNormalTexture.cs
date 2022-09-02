using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperUltraFishing
{
    public struct VertexPositionColorNormalTexture : IVertexType
    {
        public Vector3 Position;

        public Color Color;

        public Vector3 Normal;

        public Vector2 TextureCoordinate;

        public static readonly VertexDeclaration VertexDeclaration;

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        static VertexPositionColorNormalTexture()
        {
            VertexDeclaration = new VertexDeclaration
            (
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(sizeof(float) * 3 + 4, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(sizeof(float) * 6 + 4, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            );
        }

        public VertexPositionColorNormalTexture(Vector3 position, Color color, Vector2 textureCoordinate, Vector3 normal = default)
        {
            Position = position;
            Color = color;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
        }
    }
}
