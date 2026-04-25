HEADER
{
	Description = "Scanner Shader mit Colormap";
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
		PixelInput i = ProcessVertex( v );
		
		// Deine bestehende Vertex-Transformation (Skalierung)
		float scale = sin(g_flTime * 3.0f) * 0.03f + 1.0f;
		i.vPositionPs.xyz *= scale; 
		
		i.vPositionOs = v.vPositionOs.xyz * scale;
		i.vNormalOs = v.vNormalOs.xyz;

		return FinalizeVertex( i );
	}
}

//=============================================================================

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

        // --- STREIFEN LOGIK ---
        float speed = 3.0f;       // Wie schnell der Streifen fällt
        float frequency = 0.02f;  // Sehr kleine Zahl für wenige, breite Wellen
        float thickness = 0.01f;  // Schwellenwert für die Dicke des Streifens

        // i.vPositionOs.z ist die Oben/Unten-Achse in S&box.
        // Das Minus-Zeichen (-) sorgt dafür, dass er von Oben nach Unten läuft.
        float wave = sin( (g_flTime * speed) + ((i.vPositionOs.x + i.vPositionOs.y + i.vPositionOs.z) * frequency) );
        float stripeMask = smoothstep( 1.0f - thickness, 1.0f, wave );

        // --- MATERIAL ZUWEISUNG ---
        m.Albedo = colormap.rgb;
        
        // Weißer Streifen (Multiplikator 5.0f für starken Glow)
        float3 whiteStripe = float3( 1.0f, 1.0f, 1.0f ) * 5.0f; 
        m.Emission = whiteStripe * stripeMask;

		m.Opacity = 1.0f; 
		m.Roughness = 0.5f;
		m.Metalness = 0.0f;
		
		return ShadingModelStandard::Shade( m );
	}
}