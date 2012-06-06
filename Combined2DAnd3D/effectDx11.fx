Texture2D g_Overlay;
 
SamplerState g_samLinear
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};
 
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
// Used as an overlay
// ------------------------------------------------------
 
struct OverlayVS_IN
{
	float4 pos : POSITION;
	float2 tex : TEXCOORD0;
};
 
struct OverlayPS_IN
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD0;
};
 
OverlayPS_IN OverlayVS( OverlayVS_IN input )
{
	OverlayPS_IN output = (OverlayPS_IN)0;
	output.pos = input.pos;
	output.tex = input.tex;
	return output;
}
 
float4 OverlayPS( OverlayPS_IN input ) : SV_Target
{
	float4 color =  g_Overlay.Sample(g_samLinear, input.tex);
	return color + float4(0.8,0.8,0.8,1);;
}
 
// ------------------------------------------------------
// Techniques
// ------------------------------------------------------
 
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
		SetVertexShader( CompileShader( vs_4_0, OverlayVS() ) );
		SetPixelShader( CompileShader( ps_4_0, OverlayPS() ) );
	}
}
