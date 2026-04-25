cbuffer cbPerFrame : register(b0)
{
    float4x4 matVP;
    float4x4 matGeo;
    float uTime; // Die Zeit-Variable muss auch hier definiert sein
};

struct PSInput
{
	float4 Color : COLOR;
	float3 WorldPos : TEXCOORD0; // Muss mit VSOutput übereinstimmen
};

float4 main(PSInput pin) : SV_TARGET

{
	// Wir nehmen den Sinus hoch 10. 
	// Kleine Werte werden fast Null, nur die Spitzen bleiben hell.
	float wave = pow(sin((uTime - pin.WorldPos.y) * 5.0f) * 0.5f + 0.5f, 5.0f);

    // 2. Wir definieren eine Wunschfarbe für den Streifen.
    // Beispiel: Ein schönes Orange/Gold (R=1.0, G=0.7, B=0.0)
    float4 stripeColor = float4(3.0f, 3.0f, 3.0f, 1.0f);

    // 3. Wir multiplizieren die Welle mit der Farbe
    float4 waveColor = stripeColor * wave;
    float4 baseColor = float4(pin.Color.rgb, 0.1f);
	float4 finalColor = baseColor * 0.5f + waveColor * 0.5f;
	
    return finalColor;
}

