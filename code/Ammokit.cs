using Sandbox;
using System;

public sealed class Ammokit : Component, Component.ITriggerListener
{
	[Property] float RocketAmount = 5f;

	SceneTraceResult GroundTrace;

	Vector3 MittelPunkt;
	float SchwebeDistance = 50;
	float SchwebeFrequenz = 1f;

	protected override void OnStart()
	{
		GroundTrace = Scene.Trace
			.Ray( WorldPosition, WorldPosition + Vector3.Down * 10000 )
			.Radius( 1 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "player", "dead" )
			.Run();

		MittelPunkt = GroundTrace.EndPosition + Vector3.Up * 80;

		GameObject.WorldPosition = MittelPunkt;
	}

	protected override void OnFixedUpdate()
	{
		// Rotiert das Kit langsam um die Y-Achse
		// Zeit schiebt Sinusfunktion(Welle) voran, multipliziert mit Frequenz für enge oder weite Wellen, dann mit Distanz multiplizieren und auf MittelPunkt addieren

		Transform transform = new Transform(
			MittelPunkt + Vector3.Up * (MathF.Sin( Time.Now * SchwebeFrequenz ) * SchwebeDistance),
			Rotation.From( 0, GameObject.WorldRotation.Yaw() + 0.2f, 0 ),
			WorldScale
		);

		WorldTransform = transform;
	}

	public void OnTriggerEnter( GameObject other )
	{
		if ( !other.Tags.Has( "player" ) ) return;
		Log.Info( $"Collided with {other.Name}" );
		
		other.GetComponentInParent<GunControl>().CurrentRockets += RocketAmount;
		Sound.Play( "sounds/medikitsound.sound", WorldPosition );

		GameObject.Parent.Destroy();

	}
}
