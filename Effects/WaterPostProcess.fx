sampler uImage0 : register(s0); // The contents of the screen

texture DistortMap;
sampler2D DistortMapSample = sampler_state
{
	texture = <DistortMap>;
	AddressU = Wrap;
	AddressV = Wrap;
	MipFilter = LINEAR;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
};

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
	float4 distortTargetColor = tex2D(DistortMapSample, uv);
	if(distortTargetColor.g > 0)
		return tex2D(uImage0, uv + distortTargetColor.r);
	return tex2D(uImage0, uv);
}

technique Technique1
{
	pass PostProcessPass
	{
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
};
