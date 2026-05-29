using Sandbox;
using Sandbox.Audio;
using Sandbox.Modals;
using System;
using System.Diagnostics;
using System.Net.Quic;
using System.Numerics;
using System.Threading.Tasks;
using static HealthSystem;
using static Sandbox.ModelPhysics;

public sealed class GunControl : Component, HealthSystem.IHealthEvent
{
	SceneLoader SceneLoader;
	HighscoreManager HighscoreManager;

	[Property] Rigidbody CarBody;
	[Property] GameObject Turret;
	[Property] GameObject Muzzle;
	[Property] CameraComponent MainCamera;
	[Property] GameObject Crosshair;
	[Property] BeamEffect ShootBeam;
	[Property] BeamEffect LaserBeam;
	[Property] GameObject BulletHole;
	[Property] GameObject BulletSpark;
	[Property] GameObject BulletSparkEnemy;
	[Property] SpriteRenderer MuzzleFlashSprite;
	[Property] ParticleEffect MuzzleFlashEffect;
	[Property] ParticleSphereEmitter MuzzleFlashEmitter;
	[Property] float aimRange = 15000f;
	[Property] float aimWidth = 2000f;
	[Property] float ShootCooldown = 0.2f;
	[Property] float Inaccuracy = 0.015f;
	TimeUntil NextShot;

	[Property] GameObject Rocket;
	[Property] float RocketSpeed = 10000f;
	[Property] public float StartRockets = 15;
	public float CurrentRockets;

	SceneTraceResult SightlineTrace;
	bool IsAiming;

	GameObject newBulletHole;
	GameObject newBulletSpark;
	GameObject newBulletSparkEnemy;

	TimeSince SinceAim;
	TimeSince SinceGunAnimationStart;
	Vector3 originalGunScale;

	Game.Overlay overlay = new Game.Overlay();
	Random random = new Random();

	void IHealthEvent.OnDeath()
	{
		Log.Info( "PLAYER DIED" );
		Sound.Play( "sounds/player-dead-8bit.sound", CarBody.WorldPosition );

		HighscoreManager.WriteToLeaderboard();
		// HighscoreManager.ResetScore();

		DeathVignette();

		Scene.TimeScale = 0.3f;
		SceneLoader.SceneLoadOptions.SetScene( SceneLoader.LobbyScene );
		SceneLoader.StartCountdown( 0, 3 );
	}


	protected override void OnStart()
	{

		SceneLoader = Scene.Get<SceneLoader>();
		HighscoreManager = Scene.Get<HighscoreManager>();

		originalGunScale = Turret.WorldScale;
		MuzzleFlashSprite.Enabled = false;

		CurrentRockets = StartRockets;
	}

	protected override void OnUpdate()
	{
		// Ramming();
		// Wo man hinaimed


		DebugOverlay.Sphere( new Sphere( MainCamera.WorldPosition + MainCamera.WorldRotation.Forward * aimRange, aimWidth ) );


		Crosshair.WorldPosition = MainCamera.WorldPosition + MainCamera.WorldRotation.Forward * aimRange / 20;

		if (!IsAiming)
		{
			LaserBeam.TargetGameObject = null;
			LaserBeam.TargetPosition = Crosshair.WorldPosition;
		}

		if ( SinceAim > 0.1f ) 
		{
			SinceAim = 0;
			IsAiming = false;

			Rotation coneTraceRotation = Rotation.LookAt( MainCamera.WorldRotation.Up, MainCamera.WorldRotation.Backward ); //CarBody.WorldRotation.Angles().WithPitch(90);
			IEnumerable<SceneTraceResult> AimConeTrace = Scene.Trace.Cone( aimRange, aimWidth,
				MainCamera.WorldPosition + MainCamera.WorldRotation.Forward * aimRange * 0.5f,
				MainCamera.WorldPosition + MainCamera.WorldRotation.Forward * aimRange * 0.501f )
				.Rotated( coneTraceRotation )
				.WithTag( "enemy" )
				.WithoutTags( "dead" )
				.IgnoreGameObjectHierarchy( GameObject )
				.RunAll();

			if ( AimConeTrace.Any() )
			{
				Log.Info( $"Found {AimConeTrace.Count()} targets in aim cone" );
				Vector3 targetPos = Vector3.Zero;

				AimConeTrace = AimConeTrace.OrderBy( x => Muzzle.WorldPosition.Distance( x.GameObject.WorldPosition ) );
				foreach ( SceneTraceResult hit in AimConeTrace )
				{
					Ray TurretRay = new Ray( Muzzle.WorldPosition, (hit.GameObject.WorldPosition + hit.GameObject.WorldRotation.Up * 200) - Muzzle.WorldPosition );
					SightlineTrace = Scene.Trace.Ray( TurretRay, aimRange )
						.IgnoreGameObjectHierarchy( GameObject )
						.Run();
					//DebugOverlay.Trace( SightlineTrace );

					if ( SightlineTrace.GameObject.Tags.Has( "enemy" ) && !SightlineTrace.GameObject.Tags.Has( "dead" ) )
					{
						LaserBeam.TargetPosition = hit.GameObject.WorldPosition + Vector3.Up * 100;
						IsAiming = true;
						break;
					}

				}

			}
			else
			{
				Ray TurretRay = new Ray( Muzzle.WorldPosition, MainCamera.WorldRotation.Forward );
				SightlineTrace = Scene.Trace.Ray( TurretRay, aimRange )
				.IgnoreGameObjectHierarchy( GameObject )
				.Run();
			}
		}

		// DebugOverlay.Trace( ShootTrace );

		// Turret Yaw mit Camera Yaw mitdrehen
		Turret.WorldRotation = Rotation.LookAt( MainCamera.WorldRotation.Backward, CarBody.WorldRotation.Up );

		if ( Input.Down( "attack1" ) && NextShot )
		{
			// Log.Info( "Shooting" );
			Sound.Play( "sounds/bullet-ricochet.sound", Muzzle.WorldPosition );
			MuzzleFlashEmitter.Emit( MuzzleFlashEffect );
			FlashMuzzleSprite( 0.05f );
			AnimateGun( 0.03f);

			if ( SightlineTrace.Hit )
			{

				newBulletHole = BulletHole.Clone( SightlineTrace.HitPosition, Rotation.LookAt( SightlineTrace.Normal, Vector3.Up ) );
				if ( SightlineTrace.GameObject.IsValid()) newBulletHole.SetParent( SightlineTrace.GameObject );

				if ( SightlineTrace.GameObject.Tags.Has( "enemy" ) && !SightlineTrace.GameObject.Tags.Has("dead") )
				{
					if ( SightlineTrace.GameObject.GetComponent<HealthSystem>().IsValid() ) SightlineTrace.GameObject.GetComponent<HealthSystem>().Damage( 50f );

					if ( SightlineTrace.GameObject.GetComponent<ZombieBrain>() != null )
					{
						SightlineTrace.GameObject.GetComponent<ZombieBrain>().CurrentState = ZombieState.Staggered;
						SightlineTrace.GameObject.GetComponent<ZombieBrain>().KnockBack = Math.Max( 0.1f, SightlineTrace.GameObject.GetComponent<ZombieBrain>().KnockBack + 0.1f );
						SightlineTrace.GameObject.GetComponent<ZombieBrain>().AnimateHit();

					}
					else if ( SightlineTrace.GameObject.GetComponent<VampireBrain>() != null )
					{
						SightlineTrace.GameObject.GetComponent<VampireBrain>().CurrentState = VampireState.Staggered;
						SightlineTrace.GameObject.GetComponent<VampireBrain>().UntilKnockBack = Math.Max( 0.1f, SightlineTrace.GameObject.GetComponent<VampireBrain>().UntilKnockBack + 0.1f );

						SightlineTrace.GameObject.WorldRotation = SightlineTrace.GameObject.WorldRotation.Angles().WithYaw( SightlineTrace.GameObject.WorldRotation.Yaw() + random.Float( -10, 10 ) );
						SightlineTrace.GameObject.GetComponent<VampireBrain>().TargetPosition += SightlineTrace.Direction * 200;
						SightlineTrace.GameObject.GetComponent<VampireBrain>().AnimateHit();
						// Rotation?
					}
					else if ( SightlineTrace.GameObject.GetComponent<GhostBrain>() != null )
					{
						SightlineTrace.GameObject.GetComponent<GhostBrain>().CurrentState = GhostState.Staggered;
						SightlineTrace.GameObject.GetComponent<GhostBrain>().UntilKnockBack = Math.Max( 0f, SightlineTrace.GameObject.GetComponent<GhostBrain>().UntilKnockBack + 0f ); // 0f = disabled

						SightlineTrace.GameObject.GetComponent<GhostBrain>().AnimateHit();
						// ShootTrace.GameObject.GetComponent<GhostBrain>().TargetPosition += ShootTrace.Direction * 200;
						// Rotation?
					}


					if ( SightlineTrace.GameObject.GetComponent<Rigidbody>() != null )
					{
						Rigidbody rb = SightlineTrace.GameObject.GetComponent<Rigidbody>();

						rb.GravityScale = 1;
						rb.SmoothRotate( rb.WorldRotation.Angles().WithPitch( random.FromArray<int>( new int[] { -10, -15 } ) )
							.WithYaw( rb.WorldRotation.Yaw() + random.FromArray<int>( new int[] { -15, 15 } ) ), 0.001f, 1f );
						// ShootTrace.GameObject.GetComponent<Rigidbody>().ApplyTorque( ShootTrace.GameObject.WorldRotation.Up * 100000 * ShootTrace.GameObject.GetComponent<Rigidbody>().Mass );
						SightlineTrace.GameObject.GetComponent<Rigidbody>().ApplyImpulse( (SightlineTrace.Direction + Vector3.Up) * 100 * SightlineTrace.GameObject.GetComponent<Rigidbody>().Mass );
					}


					// Partikel und Sound und bullethole für Gegner
					newBulletHole.GetComponent<Decal>().ColorTint = Color.Red * random.Float(3, 5); newBulletHole.WorldScale = (newBulletHole.WorldScale * 3).WithX( 4 );
					newBulletSparkEnemy = BulletSparkEnemy.Clone( SightlineTrace.HitPosition, Rotation.LookAt( SightlineTrace.Normal, Vector3.Up ) );

					Sound.Play( "sounds/bullet-impact-flesh.sound", SightlineTrace.HitPosition );
				}
				else
				{
					// normaler Partikel
					newBulletSpark = BulletSpark.Clone( SightlineTrace.HitPosition, Rotation.LookAt( SightlineTrace.Normal, Vector3.Up ) );
				}
			}

			ShootBeam.TargetPosition = SightlineTrace.EndPosition;
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
		else if ( GetComponent<HealthSystem>().CurrentHealth > 0 && Scene.TimeScale == 0 ) 
		{ 
			Scene.TimeScale = 1;
			Mixer.Master.Volume = 1.0f;
		}

	}

	async Task FlashMuzzleSprite(float time)
	{
		MuzzleFlashSprite.WorldRotation = MuzzleFlashSprite.WorldRotation.Angles().WithRoll( random.Int(-10, 15) );
		MuzzleFlashSprite.Enabled = true;
		await Task.DelayRealtimeSeconds( time );
		MuzzleFlashSprite.Enabled = false;
	}

	async Task DeathVignette()
	{
		while ( MainCamera.GetComponent<Vignette>().Intensity < 2f )
		{
			MainCamera.GetComponent<Vignette>().Intensity += Time.Delta * 3f;
			await Task.FrameEnd();
		}
	}

	void LaunchRocket() 
	{
		if ( CurrentRockets <= 0 ) return; // Sound
		AnimateGun( 0.3f );
		Sound.Play( "sounds/grenadelauncher.sound", Muzzle.WorldPosition );

		GameObject newRocket = Rocket.Clone( Muzzle.WorldPosition, Rotation.LookAt( MainCamera.WorldRotation.Forward, Vector3.Up ) );
		newRocket.GetComponentInChildren<Rigidbody>().Velocity = (SightlineTrace.EndPosition - Muzzle.WorldPosition).Normal * RocketSpeed;
	
		CurrentRockets--;
	}

	public async Task AnimateGun(float strength)
	{
		float duration = 0.1f;
		SinceGunAnimationStart = 0;

		Turret.WorldScale = originalGunScale.z * (1f + strength);

		while ( SinceGunAnimationStart < duration )
		{
			Turret.WorldScale = Turret.WorldScale.z.LerpTo( originalGunScale.z, 0.1f );
			await Task.FrameEnd();
		}

		if ( !this.IsValid() ) return;
		Turret.WorldScale = originalGunScale;
	}

	/*
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
	}*/
}
