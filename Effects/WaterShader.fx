texture LargePerlinTexture;
sampler2D LargePerlin = sampler_state
{
	texture = <LargePerlinTexture>;
	AddressU = Wrap;
	AddressV = Wrap;
	MipFilter = LINEAR;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
};

texture SmallPerlinTexture;
sampler2D SmallPerlin = sampler_state
{
	texture = <SmallPerlinTexture>;
	AddressU = Wrap;
	AddressV = Wrap;
	MipFilter = LINEAR;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
};

float DepthScale;

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
	float4 PositionOut : TEXCOORD1;//dupicated because you cannot access the position output from the PS in PS_2_0
	float4 Color : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	float4 pos = mul(input.Position, WorldViewProjection);
	output.Position = pos;
	output.PositionOut = pos;

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

	float2 distortColor = float2(((colorPerlin.g - (colorSmallPerlin.g * 0.40)) * Strength) / (input.PositionOut.z * DepthScale), 1);
	output.Extra = float4(distortColor, 0, 1);

	float distFromCenter = length(input.PositionOut.xy - float2(0.5, 0.5));

	float4 outputColor = input.Color * colorPerlin;//disabled to debug, may move to post processing
	if (distFromCenter < ShineSize) {
		outputColor.rgb = lerp(outputColor.rgb, float3(1,1,1), (trunc(distortColor.r + ShineLevel) * ((-distFromCenter + ShineSize) / ShineSize)));
	}
	output.Color = outputColor;
	//output.Color = float4(0, 0, 0, 0);

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
