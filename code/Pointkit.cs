using Sandbox;
using System;

public sealed class Pointkit : Component, Component.ITriggerListener
{
	[Property] float PointAmount = 500f;

	HighscoreManager HighscoreManager;

	SceneTraceResult GroundTrace;

	Vector3 MittelPunkt;
	float SchwebeDistance = 50;
	float SchwebeFrequenz = 1f;

	protected override void OnStart()
	{
		HighscoreManager = Scene.Get<HighscoreManager>();

		// Checkt ground und setzt Mittelpunkt
		GroundTrace = Scene.Trace
			.Ray( WorldPosition, WorldPosition + Vector3.Down * 10000 )
			.Radius( 1 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "player", "dead" )
			.Run();

		MittelPunkt = GroundTrace.EndPosition + Vector3.Up * 80;

		GameObject.WorldPosition = MittelPunkt;
	}

	protected override void OnUpdate()
	{
		// Rotiert das Kit langsam um die Y-Achse
		GameObject.WorldRotation = Rotation.From( 0, GameObject.WorldRotation.Yaw() + 0.2f, 0 );

		// Zeit schiebt Sinusfunktion(Welle) voran, multipliziert mit Frequenz für enge oder weite Wellen, dann mit Distanz multiplizieren und auf MittelPunkt addieren
		GameObject.WorldPosition = MittelPunkt + Vector3.Up * (MathF.Sin( Time.Now * SchwebeFrequenz ) * SchwebeDistance);
	}

	public void OnTriggerEnter( GameObject other )
	{
		if ( !other.Tags.Has( "player" ) ) return;
		Log.Info( $"Collided with {other.Name}" );

		HighscoreManager.IncreaseScore( PointAmount );
		Sound.Play( "sounds/medikitsound.sound", WorldPosition );

		GameObject.Parent.Destroy();

	}
}
