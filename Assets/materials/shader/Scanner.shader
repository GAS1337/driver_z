HEADER
{
	Description = "Scanner Shader";
}

FEATURES
{
    // Hier können später Features aktiviert werden
}

MODES
{
    VrForward();
    Depth( "vr_depth_only.vfx" );
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
	float3 vPositionOs : POSITION < Semantic( PosXYZ ); >;
	float3 vNormalOs : NORMAL < Semantic( Normal ); >;
};

struct VertexOutput
{
	float4 vPositionPs : SV_POSITION;
	float4 vColor : COLOR0;
	float3 vLocalPos : TEXCOORD0;
};

VS
{
	#include "common/vertex.hlsl"

	VertexOutput MainVs( VertexInput i )
	{
		VertexOutput o = BuildVertexOutput( i );
		
		// Deine Logik aus MainVS.hlsl
		float scale = sin(g_flTime * 3.0f) * 0.03f + 1.0f;
		float3 animatedPos = i.vPositionOs * scale;

		o.vPositionPs = PositionWithDefaultNoops( animatedPos );
		o.vColor = float4( abs( i.vNormalOs ), 1.0 );
		o.vLocalPos = animatedPos;  
		
		return o;
	}
}

PS
{
    #include "common/pixel.hlsl"

	float4 MainPs( VertexOutput i ) : SV_Target0
	{
		// Deine Logik aus MainPS.hlsl
		float wave = pow( sin( (g_flTime - i.vLocalPos.y) * 5.0f ) * 0.5f + 0.5f, 5.0f );
		float4 stripeColor = float4( 3.0f, 3.0f, 3.0f, 1.0f );
		
		float4 waveColor = stripeColor * wave;
		float4 baseColor = float4( i.vColor.rgb, 0.1f );
		
		return baseColor * 0.5f + waveColor * 0.5f;
	}
}