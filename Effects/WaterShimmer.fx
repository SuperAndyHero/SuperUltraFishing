texture inputTexture;
sampler2D inputSampler = sampler_state 
{ 
	texture = <inputTexture>; 
	AddressU = Wrap;
	AddressV = Wrap; 
};

texture LargePerlinTexture;
sampler2D LargePerlin = sampler_state 
{ 
	texture = <inputTexture>; 
	AddressU = Wrap;
	AddressV = Wrap; 
};

texture SmallPerlinTexture;
sampler2D SmallPerlin = sampler_state 
{ 
	texture = <inputTexture>; 
	AddressU = Wrap;
	AddressV = Wrap; 
};

float2 Offset;
float Strength;
float ShineSize;
float ShineLevel;

struct VertexShaderOutput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

float4 main(VertexShaderOutput input) : COlOR
{
	float2 uvs = input.TextureCoordinates;

	float2 OffsetPerlin = frac(uvs + min(1, Offset));
	float2 OffsetSmallPerlin = frac(uvs + min(1, -Offset));

	float4 colorPerlin = tex2D(LargePerlin, float2(OffsetPerlin.x,  OffsetPerlin.y));
	float4 colorSmallPerlin = tex2D(SmallPerlin, float2(OffsetSmallPerlin.x, OffsetSmallPerlin.y));

	float distFromCenter = length(uvs - float2(0.5, 0.5));


	float4 outputColor = tex2D(inputSampler, frac(uvs + ((colorPerlin.rg - (colorSmallPerlin.rg * 0.33)) * Strength)));
	outputColor = lerp(outputColor, float4(input.Color.rgb, 1), (colorPerlin * input.Color.a));
	if(distFromCenter < ShineSize) {
		outputColor += (trunc(colorPerlin + (colorSmallPerlin.r * 0.5) + ShineLevel) * ((-distFromCenter + ShineSize) / ShineSize));
	}
	return outputColor;
}

technique SpriteDrawing
{
    pass ShimmerPass
    {
        PixelShader = compile ps_2_0 main();
    }
};
