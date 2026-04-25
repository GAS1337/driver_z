cbuffer cbPerFrame : register(b0)
{
	float4x4 matVP;
	float4x4 matGeo;
	float uTime;
};

struct VSInput
{
	float3 Position : POSITION;
	float3 Normal : NORMAL;
};

struct VSOutput
{	
	float4 Position : SV_POSITION;
	float4 Color : COLOR;
	float3 WorldPos : TEXCOORD0; // Neu: Wir geben die Position weiter
};

VSOutput main(VSInput vin)
{
VSOutput vout = (VSOutput)0;

	// 1. Wir berechnen einen Skalierungsfaktor basierend auf der Zeit
	// sin(uTime) gibt Werte zwischen -1 und 1. 
	// Wir rechnen (+ 1.5), damit der Würfel nie ganz verschwindet.
	float scale = sin(uTime * 3.0f) * 0.03f + 1.0f;

	// 2. Wir multiplizieren die lokale Position mit diesem Faktor
	float3 animatedPos = vin.Position * scale;

	// 3. Die Transformation in den 3D-Raum (jetzt mit animatedPos)
	vout.Position = mul(mul(float4(animatedPos, 1.0f), matGeo), matVP);
	
	// Wir behalten die Normalen-Farbe bei
	vout.Color = float4(abs(vin.Normal), 1);
	// Hier speichern wir die lokale Position
	vout.WorldPos = animatedPos; 
	
	return vout;
}