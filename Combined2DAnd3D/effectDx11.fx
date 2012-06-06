Texture2D g_Overlay;
 
SamplerState g_samLinear
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};
 
// ------------------------------------------------------
// A very simple shader
// ------------------------------------------------------
 
float4 SimpleVS(float4 position : POSITION) : SV_POSITION
{
	return position;
}
 
float4 SimplePS(float4 position : SV_POSITION) : SV_Target
{
	return float4(1.0f, 1.0f, 0.0f, 1.0f);
}
 
// ------------------------------------------------------
// A shader that accepts Position and Color
// ------------------------------------------------------
 
struct ColorVS_IN
{
	float4 pos : POSITION;
	float4 col : COLOR;
};
 
struct ColorPS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};
 
ColorPS_IN ColorVS( ColorVS_IN input )
{
	ColorPS_IN output = (ColorPS_IN)0;
	output.pos = input.pos;
	output.col = input.col;
	return output;
}
 
float4 ColorPS( ColorPS_IN input ) : SV_Target
{
	return input.col;
}
 
// ------------------------------------------------------
// A shader that accepts Position and Texture
// Used as a text overlay
// ------------------------------------------------------
 
struct TextVS_IN
{
	float4 pos : POSITION;
	float2 tex : TEXCOORD0;
};
 
struct TextPS_IN
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD0;
};
 
TextPS_IN TextVS( TextVS_IN input )
{
	TextPS_IN output = (TextPS_IN)0;
	output.pos = input.pos;
	output.tex = input.tex;
	return output;
}
 
float4 TextPS( TextPS_IN input ) : SV_Target
{
	float4 color =  g_Overlay.Sample(g_samLinear, input.tex);
	return color;
}
 
// ------------------------------------------------------
// Techniques
// ------------------------------------------------------
 
technique11 Simple
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, SimpleVS() ) );
		SetPixelShader( CompileShader( ps_4_0, SimplePS() ) );
	}
}
 
technique11 Color
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, ColorVS() ) );
		SetPixelShader( CompileShader( ps_4_0, ColorPS() ) );
	}
}
 
technique11 Overlay
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, TextVS() ) );
		SetPixelShader( CompileShader( ps_4_0, TextPS() ) );
	}
}
