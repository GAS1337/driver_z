HEADER
{
	Description = "Scanner Shader (Statisch mit Colormap)";
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
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
};

//=============================================================================

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		// Standard-Transformation ohne Skalierung
		PixelInput i = ProcessVertex( v );
		
		// Wir reichen die originalen Objekt-Koordinaten einfach weiter
		i.vPositionOs = v.vPositionOs.xyz;
		i.vNormalOs = v.vNormalOs.xyz;

		return FinalizeVertex( i );
	}
}

//=============================================================================

PS
{
    #include "common/pixel.hlsl"

    // Material-Editor Eingaben
    CreateInputTexture2D( ColorTexture, Srgb, 8, "None", "_color", "Albedo", DefaultFile( "materials/default/default_color.tga" ) );
    Texture2D g_tColor < Channel( RGBA, Box( ColorTexture ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
    SamplerState g_sSampler < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::Init( i );
		
        // Colormap sampeln
        float4 colormap = Tex2DS( g_tColor, g_sSampler, i.vTextureCoords.xy );

        // --- STREIFEN LOGIK ---
        // Parameter für den einzelnen Streifen
        float speed = 5.0f;       
        float frequency = 0.03f;  
        float thickness = 0.04f; 

        // Animation basierend auf der vertikalen Z-Achse (Object Space)
        float wave = sin( (g_flTime * speed) + ((i.vPositionOs.x + i.vPositionOs.y + i.vPositionOs.z) * frequency) );
        float stripeMask = smoothstep( 1.0f - thickness, 1.0f, wave );

        // --- MATERIAL ZUWEISUNG ---
        m.Albedo = colormap.rgb;
        
        // Weißer Streifen als Emission (Glow)
        float3 whiteStripe = float3( 1.0f, 0.0f, 0.0f ) * 1.0f; 
        m.Emission = whiteStripe * stripeMask;

		m.Opacity = 1.0f; 
		m.Roughness = 0.5f;
		m.Metalness = 0.0f;
		
		return ShadingModelStandard::Shade( m );
	}
}