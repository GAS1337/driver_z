HEADER
{
	Description = "Low Poly Baum mit Simplex Wind";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth();
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#include "common/shared.hlsl"
    #include "procedural.hlsl" // Wichtig für Simplex2D

    struct VertexInput
    {
        #include "common/vertexinput.hlsl"
    };

    struct PixelInput
    {
        #include "common/pixelinput.hlsl"
        float3 vPositionOs : TEXCOORD14;
    };
}

VS
{
    #include "common/vertex.hlsl"

    PixelInput MainVs( VertexInput v )
    {
        // 1. Standard Transformation
        PixelInput i = ProcessVertex( v );
        
        // --- PARAMETER FÜR SANFTES BIEGEN ---
        float3 windDirection = float3( 1.0f, 0.5f, 0.0f ); 
        float windSpeed = 0.15f;        
        float windFrequency = 0.00008f;  
        float swayAmplitude = 50.0f;   

        // 2. Welt-Sway Logik
        float worldSample = dot( i.vPositionWs.xy, windDirection.xy ) * windFrequency;
        float windGust = Simplex2D( float2( worldSample - g_flTime * windSpeed, 0.0 ) );
        windGust = saturate( windGust * 0.5f + 0.5f ); 

        float sway = sin( g_flTime * 0.8f + worldSample * 10.0f ) * 0.3f + 0.7f;
        float finalSway = sway * windGust * swayAmplitude;

        // --- PARAMETER FÜR DYNAMISCHEN DETAIL-WIGGLE ---
        float dynamicStrength = (4.5f + windGust * 0.8f) * 0.5f; 
        float dynamicSpeed = (0.3f + windGust * 0.0005f); 

        float detailFrequency = 1.0f; 
        float detailNoise = Simplex2D( v.vPositionOs.xy * detailFrequency + (g_flTime * dynamicSpeed) );
        detailNoise += sin( g_flTime * (dynamicSpeed * 1.5f) + v.vPositionOs.z ) * 0.5f;

        // 3. Invertierte Maskierung
        // '1.0f -' kehrt den Verlauf um. 
        // saturate stellt sicher, dass die Werte im Bereich [0, 1] bleiben.
        float heightMask = pow( saturate( 1.0f - ( v.vPositionOs.z * 0.008f ) ), 2.0f ); 
        
        // 4. Anwendung
        // Haupt-Sway (Wirkt jetzt unten am stärksten)
        i.vPositionWs.xyz += windDirection * finalSway * heightMask;

        // Detail-Wiggle
        i.vPositionWs.xyz += i.vNormalWs * detailNoise * dynamicStrength * heightMask;

        // 5. Projektion neu berechnen
        i.vPositionPs = Position3WsToPs( i.vPositionWs.xyz );

        i.vPositionOs = v.vPositionOs.xyz;
        return FinalizeVertex( i );
    }
}

PS
{
    #include "common/pixel.hlsl"

    CreateInputTexture2D( ColorTexture, Srgb, 8, "None", "_color", "Albedo", DefaultFile( "materials/default/default_color.tga" ) );
    Texture2D g_tColor < Channel( RGBA, Box( ColorTexture ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
    SamplerState g_sSampler < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::Init( i );
		
        float4 colormap = Tex2DS( g_tColor, g_sSampler, i.vTextureCoords.xy );

        m.Albedo = colormap.rgb;
		m.Opacity = 1.0f; 
		m.Roughness = 0.8f;
		m.Metalness = 0.0f;
		
		return ShadingModelStandard::Shade( m );
	}
}