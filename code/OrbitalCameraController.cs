using Sandbox;
using System;

public sealed class OrbitalCameraController : Component
{
	[Property, Description("Camera to control")] CameraComponent MainCamera;
	[Property, Description("Player to follow")] GameObject Player;

	[Property, Description( "Starting distance" )] int TargetDistanceToPlayer = 2000;
	[Property, Description("Starting distance")] int DistanceToPlayer = 2000;
	[Property, Description( "Vertical Offset" )] int VerticalOffset = 32;
	[Property, Description( "Minimal distance to player" )] int MinDistanceToPlayer = 600;
	[Property, Description( "Maximal distance to player" )] int MaxDistanceToPlayer = 10000;
	[Property, Description( "Units one step zooms" )] int ZoomStrength = 200;
	[Property, Description( "Units one step zooms" )] int AutoZoomStrength = 25;

	float Pitch = 30;
	float Yaw = 90;

	protected override void OnUpdate()
	{
		// Capture mouse and add to pitch and yaw angles
		Angles mouseMove = Input.AnalogLook;
		Pitch = (Pitch + mouseMove.pitch).Clamp( -10, 85 ); // Up&Down clamped in degrees
		Yaw = Yaw + mouseMove.yaw;
		Rotation rotation = Rotation.From( Pitch, Yaw, 0 );

		SceneTraceResult checkingSightline = Scene.Trace
			.Ray( MainCamera.WorldPosition, Player.WorldPosition + Vector3.Up * VerticalOffset )
			.Radius(1)
			.IgnoreGameObjectHierarchy(GameObject) // Ignores itself. Use tags depending on your setup
			.Run();
		// DebugOverlay.Trace( checkingSightline );

		// Checks for objects around cam to avoid stuttering
		SceneTraceResult checkingBehind = Scene.Trace
			.Sphere( AutoZoomStrength, MainCamera.WorldPosition, MainCamera.WorldPosition )
			.IgnoreGameObjectHierarchy( GameObject ) // Ignores itself. Use tags depending on your setup
			.Run();
		// DebugOverlay.Trace( checkingBehind );

		if ( Input.MouseWheel.y < 0 ) { TargetDistanceToPlayer = Math.Min( TargetDistanceToPlayer + ZoomStrength, MaxDistanceToPlayer ); }
		if ( Input.MouseWheel.y > 0 ) { TargetDistanceToPlayer = Math.Max( TargetDistanceToPlayer - ZoomStrength, MinDistanceToPlayer); }

		switch ((TargetDistanceToPlayer, checkingSightline.Distance, checkingSightline.Hit, checkingBehind.Hit ))
		{
			default: break;
			// Nur wenn checkingBehind false soll distance mit autozoom größer werden
			case( > 0, > 0, false, false ) when TargetDistanceToPlayer > checkingSightline.Distance + AutoZoomStrength:
				DistanceToPlayer += AutoZoomStrength;
				break;
			case ( > 0, > 0, false, _ ) when TargetDistanceToPlayer < checkingSightline.Distance - AutoZoomStrength:
				DistanceToPlayer -= AutoZoomStrength;
				break;
		}
		if ( checkingSightline.Hit ) // If sightline is blocked change distance to be in front of hit collider
		{
			DistanceToPlayer = Math.Max( (int)(checkingSightline.HitPosition - Player.WorldPosition).Length - AutoZoomStrength, MinDistanceToPlayer ); // -AutoZoomStrength cheats it infront
		}

		// Apply Position and Rotation
		MainCamera.WorldPosition = Player.WorldPosition + Vector3.Up * VerticalOffset - rotation.Forward * DistanceToPlayer; 
		MainCamera.WorldRotation = rotation;
		
	}
}
