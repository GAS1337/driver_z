using Sandbox;
using System;

public sealed class OrbitalCameraController : Component
{
	[Property, Description("Camera to control")] CameraComponent MainCamera;
	RotationControl RotationControl;
	[Property, Description("Player to follow")] GameObject Player;


	[Property, Description( "Starting distance" )] int TargetDistanceToPlayer = 300;
	[Property, Description("Starting distance")] int DistanceToPlayer = 300;
	[Property, Description( "Vertical Offset" )] int VerticalOffset = 128;
	[Property] float HorizontalOffset = 80;
	[Property, Description( "Minimal distance to player" )] int MinDistanceToPlayer = 300;
	[Property, Description( "Maximal distance to player" )] int MaxDistanceToPlayer = 500;
	[Property, Description( "Units one step zooms" )] int ZoomStrength = 0;
	[Property, Description( "Units one step zooms" )] int AutoZoomStrength = 1;

	[Property] Decal CrosshairSprite;


	float crosshairPitch = 0;
	float Pitch = 30;
	float Yaw = 180;

	protected override void OnStart() 
	{ 
		RotationControl = GameObject.GetComponent<RotationControl>();
	}

	protected override void OnUpdate()
	{
		// Capture mouse and add to pitch and yaw angles
		Angles mouseMove = Input.AnalogLook;
		Pitch = (Pitch + mouseMove.pitch).Clamp( 5, 5 );
		crosshairPitch = (crosshairPitch + mouseMove.pitch / 2).Clamp( 3, -5 ); // Up&Down clamped in degrees
		Yaw = Yaw + mouseMove.yaw;
		Rotation rotation = Rotation.From( Pitch, Yaw, 0 );

		SceneTraceResult checkingSightline = Scene.Trace
			.Ray( MainCamera.WorldPosition + MainCamera.WorldRotation.Forward * 10, Player.WorldPosition + Vector3.Up * VerticalOffset + Player.WorldRotation.Forward * HorizontalOffset )
			.Radius(1)
			.IgnoreGameObjectHierarchy(GameObject) // Ignores itself. Use tags depending on your setup
			.WithoutTags("enemy")
			.Run();
		// DebugOverlay.Trace( checkingSightline );

		// Checks for objects around cam to avoid stuttering
		SceneTraceResult checkingBehind = Scene.Trace
			.Sphere( AutoZoomStrength, MainCamera.WorldPosition, MainCamera.WorldPosition )
			.IgnoreGameObjectHierarchy( GameObject ) // Ignores itself. Use tags depending on your setup
			.Run();
		// DebugOverlay.Trace( checkingBehind );

		//Zoom
		if ( Input.MouseWheel.y < 0 ) { TargetDistanceToPlayer = Math.Min( TargetDistanceToPlayer + ZoomStrength, MaxDistanceToPlayer ); }
		if ( Input.MouseWheel.y > 0 ) { TargetDistanceToPlayer = Math.Max( TargetDistanceToPlayer - ZoomStrength, MinDistanceToPlayer); }

		// Zoom out when GroundCheck fails
		if ( !RotationControl.groundCheck.Hit ) 
		{ 
			TargetDistanceToPlayer = MaxDistanceToPlayer;
			VerticalOffset = Math.Max( VerticalOffset - AutoZoomStrength, 64 ); ;
		}
		// When on ground zoom depending on speed
		else 
		{
			TargetDistanceToPlayer = (int)Player.GetComponent<Rigidbody>().Velocity.Length.Remap( 0, 4000, MinDistanceToPlayer, MaxDistanceToPlayer ); ; 
			VerticalOffset = Math.Min(VerticalOffset + AutoZoomStrength, 170); 
		}
		

		switch ((TargetDistanceToPlayer, checkingSightline.Distance, checkingSightline.Hit ))
		{
			default: break;
			// Nur wenn checkingBehind false soll distance mit autozoom größer werden
			case( > 0, > 0, false ) when TargetDistanceToPlayer > checkingSightline.Distance + AutoZoomStrength:
				DistanceToPlayer += AutoZoomStrength;
				break;
			case ( > 0, > 0, false) when TargetDistanceToPlayer < checkingSightline.Distance - AutoZoomStrength:
				DistanceToPlayer -= AutoZoomStrength;
				break;
		} 
		if ( checkingSightline.Hit ) // If sightline is blocked change distance to be in front of hit collider
		{
			DistanceToPlayer = Math.Max( (int)(checkingSightline.HitPosition - Player.WorldPosition).Length - AutoZoomStrength, MinDistanceToPlayer ); // -AutoZoomStrength cheats it infront
		}

		// Apply Position and Rotation
		MainCamera.WorldPosition = Player.WorldPosition + Vector3.Up * VerticalOffset + Player.WorldRotation.Forward * HorizontalOffset - rotation.Forward * DistanceToPlayer; 
		MainCamera.WorldRotation = rotation;
		

		// 1000 is distance of crosshair to player, VerticalOffset/2
		Rotation rot = rotation.Angles().WithPitch( crosshairPitch );
		Vector3 crosshairDir = Player.WorldPosition + Vector3.Up * VerticalOffset/2 + new Vector3(rot.Forward.x, rot.Forward.y, rot.Forward.z ).Normal * 18000;
		SceneTraceResult crosshairCheck = Scene.Trace
			.Ray( Player.WorldPosition + Vector3.Up * VerticalOffset / 2,  crosshairDir )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();
		CrosshairSprite.WorldPosition = crosshairCheck.EndPosition;
		CrosshairSprite.WorldRotation = Rotation.LookAt( crosshairCheck.Normal, Vector3.Up );
	}
}
