using Sandbox;

public sealed class GunControl : Component
{
	[Property] SkinnedModelRenderer TurretRenderer;
	[Property] CameraComponent MainCamera;
	[Property] BeamEffect ShootEffect;
	[Property] float ShootCooldown = 0.33f;
	TimeUntil NextShot;

	[Property] GameObject Rocket;
	[Property] float RocketSpeed = 10000f;

	SceneTraceResult ShootTrace;

	protected override void OnUpdate()
	{
		// Wo man hinaimed
		Ray CameraRay = Scene.Camera.ScreenPixelToRay( new Vector2( Screen.Width * 0.5f, Screen.Height * 0.5f ) );
		ShootTrace = Scene.Trace.Ray( CameraRay, 10000f )
			.Radius( 8 )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();
		DebugOverlay.Trace( ShootTrace );

		// Turret Yaw mit Camera Yaw mitdrehen
		TurretRenderer.GetBoneObject( 1 ).WorldRotation = Rotation.LookAt( MainCamera.WorldRotation.Right, Vector3.Up );

		if ( Input.Down( "attack1" ) && NextShot )
		{
			Log.Info( "Shooting" );
			Sound.Play( "sounds/bullet-ricochet.sound", TurretRenderer.GetBoneObject( 1 ).WorldPosition );

			// Wenn Maus1 dann BeamSpawnen und  TargetPos setzen 
			ShootEffect.SpawnBeam();
			if ( ShootTrace.Hit ) { ShootEffect.TargetPosition = ShootTrace.HitPosition; }
			else 
			{ 
				//ShootEffect.TargetPosition = TurretRenderer.GetBoneObject( 1 ).WorldPosition + TurretRenderer.GetBoneObject( 1 ).WorldRotation.Left * 10000f; 
				ShootEffect.TargetPosition = ShootTrace.EndPosition;
			}

			NextShot = ShootCooldown;
		}

		if ( Input.Pressed( "attack2" ) ) 
		{
			Log.Info( "Rocket" );

			// 
			LaunchRocket();
		}
	}

	void LaunchRocket() 
	{
		Sound.Play( "sounds/grenadelauncher.sound", TurretRenderer.GetBoneObject( 1 ).WorldPosition );

		GameObject newRocket = Rocket.Clone( TurretRenderer.GetBoneObject( 1 ).WorldPosition, Rotation.LookAt( MainCamera.WorldRotation.Forward, Vector3.Up ) );
		newRocket.GetComponentInChildren<Rigidbody>().Velocity = (ShootTrace.EndPosition - TurretRenderer.GetBoneObject( 1 ).WorldPosition).Normal * RocketSpeed;
	}
}
