using Sandbox;
using Sandbox.Audio;
using Sandbox.Modals;
using System;
using static HealthSystem;

public sealed class GunControl : Component, HealthSystem.IHealthEvent
{
	SceneLoader SceneLoader;
	HighscoreManager HighscoreManager;

	[Property] Rigidbody CarBody;
	[Property] GameObject Turret;
	[Property] GameObject Muzzle;
	[Property] CameraComponent MainCamera;
	[Property] Decal CrosshairDecal;
	[Property] BeamEffect ShootBeam;
	[Property] GameObject BulletHole;
	[Property] GameObject BulletSpark;
	[Property] GameObject BulletSparkEnemy;
	[Property] ParticleEffect MuzzleFlashEffect;
	[Property] ParticleSphereEmitter MuzzleFlashEmitter;
	[Property] float ShootCooldown = 0.2f;
	[Property] float Inaccuracy = 0.015f;
	TimeUntil NextShot;

	[Property] GameObject Rocket;
	[Property] float RocketSpeed = 10000f;
	[Property] public float StartRockets = 15;
	public float CurrentRockets;

	SceneTraceResult ShootTrace;

	GameObject newBulletHole;
	GameObject newBulletSpark;
	GameObject newBulletSparkEnemy;


	Game.Overlay overlay = new Game.Overlay();
	Random random = new Random();

	void IHealthEvent.OnDeath()
	{
		Log.Info( "PLAYER DIED" );
		Sound.Play( "sounds/player-dead-8bit.sound", CarBody.WorldPosition );

		HighscoreManager.WriteToLeaderboard();
		// HighscoreManager.ResetScore();

		Scene.TimeScale = 0.3f;
		SceneLoader.SceneLoadOptions.SetScene( SceneLoader.LobbyScene );
		SceneLoader.StartCountdown( 0, 3);
	}

	protected override void OnStart()
	{
		SceneLoader = Scene.Get<SceneLoader>();
		HighscoreManager = Scene.Get<HighscoreManager>();

		CurrentRockets = StartRockets;
	}

	protected override void OnUpdate()
	{
		// Ramming();

		// Wo man hinaimed
		Ray CameraRay = new Ray(Muzzle.WorldPosition, (CrosshairDecal.WorldPosition - Muzzle.WorldPosition).Normal + random.VectorInSphere(Inaccuracy) );
		ShootTrace = Scene.Trace.Ray( CameraRay, 20000f )
			.Radius( 8 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags("dead")
			.Run();
		// DebugOverlay.Trace( ShootTrace );

		// Turret Yaw mit Camera Yaw mitdrehen
		Turret.WorldRotation = Rotation.LookAt( MainCamera.WorldRotation.Backward, Vector3.Up );

		if ( Input.Down( "attack1" ) && NextShot )
		{
			// Log.Info( "Shooting" );
			Sound.Play( "sounds/bullet-ricochet.sound", Turret.WorldPosition );
			MuzzleFlashEmitter.Emit(MuzzleFlashEffect);

			if ( ShootTrace.Hit )
			{
				ShootBeam.TargetPosition = ShootTrace.HitPosition;

				newBulletHole = BulletHole.Clone( ShootTrace.HitPosition, Rotation.LookAt( ShootTrace.Normal, Vector3.Up ) );

				if ( ShootTrace.GameObject.Tags.Has( "enemy" ) )
				{
					if ( ShootTrace.GameObject.GetComponent<ZombieBrain>() != null )
					{
						ShootTrace.GameObject.GetComponent<ZombieBrain>().CurrentState = ZombieState.Staggered;
						ShootTrace.GameObject.GetComponent<ZombieBrain>().KnockBack = Math.Max( 0.1f, ShootTrace.GameObject.GetComponent<ZombieBrain>().KnockBack + 0.1f );
					}

					ShootTrace.GameObject.GetComponent<HealthSystem>().Damage( 50f );

					// Partikel und Sound
					newBulletSparkEnemy = BulletSparkEnemy.Clone( ShootTrace.HitPosition, Rotation.LookAt( ShootTrace.Normal, Vector3.Up ) );
					Sound.Play( "sounds/bullet-impact-flesh.sound", ShootTrace.HitPosition );
				}
				else 
				{
					// normaler Partikel
					newBulletSpark = BulletSpark.Clone( ShootTrace.HitPosition, Rotation.LookAt( ShootTrace.Normal, Vector3.Up ) );
				}
			}
			else 
			{ 
				ShootBeam.TargetPosition = ShootTrace.EndPosition; 
			}

			ShootBeam.SpawnBeam();

			NextShot = ShootCooldown;
		}

		if ( Input.Pressed( "attack2" ) ) 
		{
			// Log.Info( "Rocket" );

			// Rocket
			LaunchRocket();
		}

		if ( overlay.IsOpen )
		{
			Scene.TimeScale = 0;
			Mixer.Master.Volume = 0f;
		}
		else if ( GetComponent<HealthSystem>().CurrentHealth > 0 ) 
		{ 
			Scene.TimeScale = 1;
			Mixer.Master.Volume = 1.0f;
		}

	}

	void LaunchRocket() 
	{
		if ( CurrentRockets <= 0 ) return; // Sound
		Sound.Play( "sounds/grenadelauncher.sound", Turret.WorldPosition );

		GameObject newRocket = Rocket.Clone( Muzzle.WorldPosition, Rotation.LookAt( MainCamera.WorldRotation.Forward, Vector3.Up ) );
		newRocket.GetComponentInChildren<Rigidbody>().Velocity = (ShootTrace.EndPosition - Muzzle.WorldPosition).Normal * RocketSpeed;
	
		CurrentRockets--;
	}

	void Ramming() 
	{ 
		if (CarBody.Velocity.WithZ(0).Length > 500) 
		{
			Capsule ramCapsule = new Capsule( CarBody.WorldPosition + CarBody.WorldRotation.Up * 40 + ( CarBody.WorldRotation.Backward + CarBody.WorldRotation.Left) * 80, 
				CarBody.WorldPosition + CarBody.WorldRotation.Up * 40 + ( CarBody.WorldRotation.Backward + CarBody.WorldRotation.Right ) * 80, 50);
			DebugOverlay.Capsule( ramCapsule, Color.Red );

			var ramTrace = Scene.Trace.Capsule( ramCapsule )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithTag( "enemy" )
				.RunAll();

			foreach ( var hit in ramTrace ) 
			{
				if ( hit.GameObject.GetComponent<HealthSystem>() != null ) 
				{
					hit.GameObject.GetComponent<HealthSystem>().Damage( 10 );

					Sound.Play( "sounds/bullet-impact-flesh.sound", hit.HitPosition );
				}
			}
		}
	}
}
