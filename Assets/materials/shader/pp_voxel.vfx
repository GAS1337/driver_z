HEADER
{
    Description = "World Space Voxelizer";
    DevShader = true;
}

MODES
{
    Default();
    Forward();
}

COMMON
{
    #include "postprocess/shared.hlsl"
    // Wir binden die von dir gefundene depth.hlsl ein
    #include "depth.hlsl"
}

struct VertexInput
{
    float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
    float2 vTexCoord : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
};

struct PixelInput
{
    float2 uv : TEXCOORD0;

    #if ( PROGRAM == VFX_PROGRAM_VS )
        float4 vPositionPs : SV_Position;
    #endif

    #if ( ( PROGRAM == VFX_PROGRAM_PS ) )
        float4 vPositionSs : SV_Position;
    #endif
};

VS
{
    PixelInput MainVs( VertexInput i )
    {
        PixelInput o;
        o.vPositionPs = float4(i.vPositionOs.xy, 0.0f, 1.0f);
        o.uv = i.vTexCoord;
        return o;
    }
}

PS
{
    #include "postprocess/common.hlsl"

    // Textur des gerenderten Spiels
    Texture2D g_tColorBuffer < Attribute( "ColorBuffer" ); SrgbRead( true ); >;

    // Parameter für den Editor
    float VoxelSize < Attribute("VoxelSize"); Default(10.0f); >; // Größe der Blöcke in Units
    float EdgeDarkening < Attribute("EdgeDarkening"); Default(0.5f); >; // Wie stark Kanten betont werden

    float4 MainPs( PixelInput i ) : SV_Target0
    {
        float2 uv = i.uv;
        float2 screenPos = i.vPositionSs.xy;

        // 1. Die echte Weltposition dieses Pixels berechnen
        float3 worldPos = Depth::GetWorldPosition( screenPos );

        // 2. Voxel-Logik: Die Weltposition auf ein Raster runden (Quantisierung)
        // Wir nehmen die worldPos und teilen sie durch die VoxelSize, runden ab, und multiplizieren zurück.
        float3 voxelWorldPos = floor( worldPos / VoxelSize ) * VoxelSize + (VoxelSize * 0.5f);

        // 3. Den "Voxel-Punkt" zurück auf den Bildschirm projizieren
        // Wir müssen wissen, wo dieser 3D-Block-Mittelpunkt auf unserem 2D-Monitor liegt.
        float4 projectedPos = mul( g_matWorldToProjection, float4( voxelWorldPos, 1.0f ) );
        float2 voxelUv = (projectedPos.xy / projectedPos.w) * 0.5f + 0.5f;
        voxelUv.y = 1.0f - voxelUv.y; // Flip Y für Screen Space

        // 4. Farbe an der Stelle des Voxels abgreifen
        float4 color = g_tColorBuffer.SampleLevel( g_sTrilinearClamp, voxelUv, 0 );

        // 5. Bonus: Einfache Kantenbetonung (Fake-Shading)
        // Wir schauen, wie weit das gerundete Zentrum vom aktuellen Pixel weg ist
        float dist = distance(worldPos, voxelWorldPos);
        float edge = smoothstep( VoxelSize * 0.4f, VoxelSize * 0.5f, dist );
        color.rgb *= lerp( 1.0f, 1.0f - EdgeDarkening, edge );

        return color;
    }
}