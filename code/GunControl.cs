using Sandbox;
using System;
using static HealthSystem;

public sealed class GunControl : Component, HealthSystem.IHealthEvent
{
	[Property] SkinnedModelRenderer TurretRenderer;
	[Property] CameraComponent MainCamera;
	[Property] SpriteRenderer CrosshairSprite;
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

	void IHealthEvent.OnDeath()
	{
		Log.Error( "PLAYER DIED" );
	}

	protected override void OnUpdate()
	{
		// Wo man hinaimed
		Ray CameraRay = new Ray(TurretRenderer.WorldPosition, CrosshairSprite.WorldPosition - TurretRenderer.WorldPosition );
		ShootTrace = Scene.Trace.Ray( CameraRay, 10000f )
			.Radius( 8 )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();
		// DebugOverlay.Trace( ShootTrace );

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
					ShootTrace.GameObject.GetComponent<ZombieBrain>().CurrentState = ZombieState.Staggered;
					ShootTrace.GameObject.GetComponent<ZombieBrain>().KnockBack = Math.Max( 0.1f, ShootTrace.GameObject.GetComponent<ZombieBrain>().KnockBack + 0.1f );
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
			// Log.Info( "Rocket" );

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
