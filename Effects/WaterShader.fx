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
	texture = <LargePerlinTexture>;
	AddressU = Wrap;
	AddressV = Wrap;
};

texture SmallPerlinTexture;
sampler2D SmallPerlin = sampler_state
{
	texture = <SmallPerlinTexture>;
	AddressU = Wrap;
	AddressV = Wrap;
};

float2 Offset;
float Strength;
float ShineSize;
float ShineLevel;

matrix WorldViewProjection;
//matrix View;
//matrix Projection;


struct VertexShaderInput
{
	float2 TextureCoordinates : TEXCOORD0;
	float4 Position : POSITION0;
	float4 Color : COLOR0;
};

struct VertexShaderOutput
{
	float2 TextureCoordinates : TEXCOORD0;
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	float4 pos = mul(input.Position, WorldViewProjection);
	output.Position = pos;

	output.Color = input.Color;
	output.TextureCoordinates = input.TextureCoordinates;

	//output.worldToTangentSpace[0] = mul(normalize(float3(0, 1, 0)), World);
	//output.worldToTangentSpace[1] = mul(normalize(float3(1, 0, 0)), World);
	//output.worldToTangentSpace[2] = mul(normalize(float3(0, 0, 1)), World);

	//output.View = normalize(float4(Target, 1.0) - mul(input.Position, World));
	//output.clipSpace = pos;

	return output;
}

struct PixelShaderOutput
{
	float4 Color : COLOR0;
	float4 Extra : COLOR1;
};

PixelShaderOutput main(VertexShaderOutput input) : COlOR
{
	PixelShaderOutput output = (PixelShaderOutput)0;

	float2 uvs = input.TextureCoordinates;

	float2 OffsetPerlin = frac(uvs + min(1, Offset));
	float2 OffsetSmallPerlin = frac(uvs + min(1, -Offset));

	float4 colorPerlin = tex2D(LargePerlin, float2(OffsetPerlin.x,  OffsetPerlin.y));
	float4 colorSmallPerlin = tex2D(SmallPerlin, float2(OffsetSmallPerlin.x, OffsetSmallPerlin.y));

	float distFromCenter = length(uvs - float2(0.5, 0.5));


	float3 outputColor = tex2D(inputSampler, frac(uvs + ((colorPerlin.rg - (colorSmallPerlin.rg * 0.33)) * Strength)));
	outputColor = lerp(outputColor, input.Color.rgb, colorPerlin);
	if (distFromCenter < ShineSize) {
		outputColor += (trunc(colorPerlin + (colorSmallPerlin.r * 0.5) + ShineLevel) * ((-distFromCenter + ShineSize) / ShineSize));
	}
	output.Color = float4(outputColor, input.Color.a);
	output.Extra = float4(outputColor, input.Color.a);
	return output;
}

technique SpriteDrawing
{
	pass ShimmerPass
	{
		VertexShader = compile vs_2_0 MainVS();
		PixelShader = compile ps_2_0 main();
	}
};
