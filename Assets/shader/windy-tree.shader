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
		
        // --- PARAMETER FÜR SANFTES BIEGEN (STAMM/HAUPT-SWAY) ---
        float3 windDirection = float3( 1.0f, 0.5f, 0.0f ); 
        float windSpeed = 0.05f;        
        float windFrequency = 0.00008f;  
        float swayAmplitude = 50.0f;   

        // 2. Welt-Sway Logik
        float worldSample = dot( i.vPositionWs.xy, windDirection.xy ) * windFrequency;
        float windGust = Simplex2D( float2( worldSample - g_flTime * windSpeed, 0.0 ) );
        windGust = saturate( windGust * 0.5f + 0.5f ); // Wert zwischen 0 (kein Wind) und 1 (volle Böe)

        float sway = sin( g_flTime * 0.8f + worldSample * 10.0f ) * 0.3f + 0.7f;
        float finalSway = sway * windGust * swayAmplitude;

        // --- PARAMETER FÜR DYNAMISCHEN DETAIL-WIGGLE ---
        // Wir koppeln die Stärke und Geschwindigkeit an windGust
        // 0.8f ist das "Grund-Zittern", das bei Windstille bleibt
        float dynamicStrength = (0.8f + windGust * 0.8f) * 2.5f; 
        float dynamicSpeed = (0.3f + windGust * 0.0005f); // Wird bei Böen bis zu 2x schneller

        float detailFrequency = 1.0f; 

        // Detail-Noise mit dynamischer Geschwindigkeit
        float detailNoise = Simplex2D( v.vPositionOs.xy * detailFrequency + (g_flTime * dynamicSpeed) );
        detailNoise += sin( g_flTime * (dynamicSpeed * 1.5f) + v.vPositionOs.z ) * 0.5f;

        // 3. Maskierung
        float heightMask = pow( saturate( v.vPositionOs.z * 0.008f ), 2.0f ); 
        
        // 4. Anwendung
        // Haupt-Sway
        i.vPositionWs.xyz += windDirection * finalSway * heightMask;

        // Detail-Wiggle mit dynamischer Stärke
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