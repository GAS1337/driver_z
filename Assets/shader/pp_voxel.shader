HEADER
{
	Description = "True 3D Volume Voxelizer";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
    Default();
    Forward();
}

COMMON
{
	#include "common/shared.hlsl"

    struct VertexInput
    {
        float3 vPositionOs : POSITION < Semantic( PosXYZ ); >;
        float2 vTexCoord : TEXCOORD0 < Semantic( Uv ); >;
    };

    struct PixelInput
    {
        float4 vPositionPs : SV_POSITION;
        float2 vTexCoord : TEXCOORD0;
    };
}

//=============================================================================

VS
{
	PixelInput MainVs( VertexInput i )
	{
		PixelInput o;
		o.vPositionPs = float4(i.vPositionOs.xyz, 1.0f);
		o.vTexCoord = i.vTexCoord;
		return o;
	}
}

//=============================================================================

PS
{
    #include "postprocess/shared.hlsl"
    #include "postprocess/common.hlsl"

    Texture2D g_tColorBuffer < Attribute( "ColorBuffer" ); SrgbRead( true ); >;

    float g_flVoxelSize < Attribute("VoxelSize"); Default(10.0f); >;
    float g_flEdgeDarkening < Attribute("EdgeDarkening"); Default(0.5f); >;

    // Wir definieren den Output direkt hier im PS Block
    struct PixelOutput
    {
        float4 vColor : SV_Target0;
        float flDepth : SV_Depth; 
    };

    PixelOutput MainPs( PixelInput i )
	{
        PixelOutput o;
        float2 vScreenPos = i.vPositionPs.xy;

        // 1. Echte Weltposition und Tiefe
        float3 vWorldPos = Depth::GetWorldPosition( vScreenPos );
        float flRawDepth = Depth::GetNormalized( vScreenPos );

        // Himmel-Check (Werte nahe 1.0 ignorieren)
        if ( flRawDepth >= 0.999f ) 
        {
            o.vColor = g_tColorBuffer.SampleLevel( g_sPointClamp, i.vTexCoord, 0 );
            o.flDepth = flRawDepth;
            return o;
        }

        // 2. Voxel-Quantisierung (World Space)
        float flSize = max( 1.0f, g_flVoxelSize );
        float3 vVoxelPos = floor( vWorldPos / flSize ) * flSize + (flSize * 0.5f);

        // 3. Zurückprojektion des Voxel-Zentrums
        float3 vRelPos = vVoxelPos - g_vCameraPositionWs;
        float4 vProj = mul( g_matWorldToProjection, float4( vRelPos, 1.0f ) );
        
        // UV-Koordinaten für das Farbsampling
        float2 vVoxelUv = (vProj.xy / vProj.w) * 0.5f + 0.5f;
        vVoxelUv.y = 1.0f - vVoxelUv.y;

        // 4. Ergebnisse zuweisen
        o.vColor.rgb = g_tColorBuffer.SampleLevel( g_sPointClamp, vVoxelUv, 0 ).rgb;
        o.vColor.a = 1.0f;

        // 5. Tiefen-Manipulation für echte Kanten (Der "Voxel-Look")
        o.flDepth = vProj.z / vProj.w;

        // 6. Shading
        float3 vDiff = abs( vWorldPos - vVoxelPos );
        float fMaxDiff = max( vDiff.x, max( vDiff.y, vDiff.z ) );
        float fEdge = smoothstep( flSize * 0.43f, flSize * 0.5f, fMaxDiff );
        o.vColor.rgb *= lerp( 1.0f, 1.0f - g_flEdgeDarkening, fEdge );

        return o;
	}
}