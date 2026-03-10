using Sandbox;
using System;

public sealed class GunControl : Component
{
	[Property] SkinnedModelRenderer TurretRenderer;
	[Property] CameraComponent MainCamera;
	[Property] BeamEffect ShootEffect;
	[Property] GameObject BulletHole;
	[Property] GameObject BulletSpark;
	[Property] float ShootCooldown = 0.33f;
	TimeUntil NextShot;

	[Property] GameObject Rocket;
	[Property] float RocketSpeed = 10000f;

	SceneTraceResult ShootTrace;

	GameObject newBulletHole;
	GameObject newBulletSpark;

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

			if ( ShootTrace.Hit )
			{
				ShootEffect.TargetPosition = ShootTrace.HitPosition;

				newBulletHole = BulletHole.Clone( ShootTrace.HitPosition, Rotation.LookAt( ShootTrace.Normal, Vector3.Up ) );
				newBulletSpark = BulletSpark.Clone( ShootTrace.HitPosition, Rotation.LookAt( ShootTrace.Normal, Vector3.Up ) );

				if ( ShootTrace.GameObject.Tags.Has( "enemy" ) )
				{
					ShootTrace.GameObject.GetComponent<HealthSystem>().Damage( 50f );
				}
			}
			else 
			{ 
				ShootEffect.TargetPosition = ShootTrace.EndPosition; 
			}

			ShootEffect.SpawnBeam();

			NextShot = ShootCooldown;
		}

		if ( Input.Pressed( "attack2" ) ) 
		{
			Log.Info( "Rocket" );

			// Rocket
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
