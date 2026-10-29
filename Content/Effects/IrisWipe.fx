#if OPENGL
	#define SV_POSITION POSITION
	#define PS_SHADERMODEL ps_3_0
#else
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float Radius = 1.1;        // size of the open circle
float AspectRatio = 1.777; // keeps the circle round on wide screens
float Softness = 0.02;     // soft edge

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state { Texture = <SpriteTexture>; };

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float2 p = input.TextureCoordinates - 0.5;
	p.x *= AspectRatio;
	float a = smoothstep(Radius, Radius + Softness, length(p));
	return float4(0, 0, 0, a);   // black outside the circle, transparent inside
}

technique IrisWipe
{
	pass P0 { PixelShader = compile PS_SHADERMODEL MainPS(); }
};