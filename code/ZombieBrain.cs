using Sandbox;
using System;
using System.Security.Cryptography;
using static Ballistics;
using static HealthSystem;

public enum ZombieState { Idle, Approach, Leap, Staggered }

public sealed class ZombieBrain : Component, HealthSystem.IHealthEvent
{
	[Property] NavMeshAgent Agent;
	[Property] GameObject Player;
	[Property] Rigidbody Body;
	[Property] GameObject DeadZombie;
	[Property] TextRenderer StateDebugText;
	[Property] ParticleRingEmitter AttackParticle;
	[Property] ParticleEffect AttackEffect;
	[Property] float ApproachCooldown = 1f;
	[Property] float LeapCooldown = 5f;
	
	// --- Leap-Parameter zum Tunen ---
	[Property] float LeapFlightTime = 1.5f;         // Konstante Flugzeit in Sekunden
	[Property] float LeapMaxHeightOffset = 800f;    // Wie viel höher der Bogen gehen soll

	float DistanceToPlayer;
	Vector3 TargetPos;

	public ZombieState CurrentState;

	TimeUntil NextApproach;
	TimeUntil NextLeap;
	TimeUntil NextAttack;
	public TimeUntil KnockBack;

	Random random = new Random();

	SceneTraceResult groundCheck;

	protected override void OnStart()
	{
		CurrentState = ZombieState.Idle;
	}

	void IHealthEvent.OnDeath()
	{
		GameObject _deadClone = DeadZombie.Clone( WorldPosition, WorldRotation, WorldScale );
		foreach (GameObject child in _deadClone.Children ) 
		{ 
			child.GetComponent<Rigidbody>().ApplyImpulse( Body.Velocity + (child.WorldPosition - GameObject.WorldPosition).Normal * 1000);
			// child.Enabled = random.NextDouble() >= 0.5;
		}
	}

	protected override void OnFixedUpdate()
	{
		DistanceToPlayer = (Player.WorldPosition - WorldPosition).Length;

		switch ( CurrentState ) 
		{
			default:
				Agent.Stop();
				break;
			case ZombieState.Idle: // STATE IS IDLE
				StateDebugText.Text = "Idle";
				Agent.Stop();
				if ( DistanceToPlayer < 7000 ) { CurrentState = ZombieState.Approach; }
				break;

			case ZombieState.Approach: // STATE IS APPROACH
				StateDebugText.Text = "Approach";
				Agent.SetAgentPosition( Agent.WorldPosition );
				Agent.UpdatePosition = true;
				// Walk to Player
				Agent.MaxSpeed = 240;

				if ( NextApproach )
				{
					Agent.MoveTo( Player.WorldPosition );
					NextApproach = ApproachCooldown;
				}
				if ( Body.WorldRotation.Forward.Angle( Player.WorldPosition - GameObject.WorldPosition ) > 1 )
				{
					Body.SmoothRotate( Rotation.LookAt( Player.WorldPosition - GameObject.WorldPosition, Vector3.Up ), 0.5f, 0.01f );
				}

				if ( DistanceToPlayer > 7000 ) { CurrentState = ZombieState.Idle; }
				if ( DistanceToPlayer < 3000 ) { CurrentState = ZombieState.Leap; }
				break;

			case ZombieState.Leap: // STATE IS LEAP

				groundCheck = Scene.Trace.Ray( WorldPosition + Vector3.Up * 10, WorldPosition + Vector3.Down * 35 )
					.Radius( 48 )
					.IgnoreGameObjectHierarchy( GameObject )
					.WithoutTags( "enemy" )
					.Run();
				// DebugOverlay.Trace( groundCheck );

				if ( DistanceToPlayer > 3000 )
				{
					if (!groundCheck.Hit) 
					{
						break;
					}
					Body.GravityScale = 1f;
					CurrentState = ZombieState.Approach;
					break;
				}

				StateDebugText.Text = "Leap"; 

				if (groundCheck.Hit) 
				{
					if ( Body.WorldRotation.Forward.Angle( Player.WorldPosition - GameObject.WorldPosition ) > 1 )
					{
						Body.SmoothRotate( Rotation.LookAt( Player.WorldPosition - GameObject.WorldPosition, Vector3.Up ), 0.5f, 0.01f );
					}
				}
				else
				{
					Body.SmoothRotate( Rotation.LookAt( Body.WorldPosition + Body.WorldRotation.Forward * 10, Vector3.Up ), 0.5f, 0.01f );
					DebugOverlay.Sphere(new Sphere(TargetPos, 300 ), Color.Orange );
				}


				Agent.SetAgentPosition( Agent.WorldPosition );
				Agent.Stop();
				Agent.UpdatePosition = false;



				// === Leap-Berechnung ===
				if ( NextLeap ) 
				{ 
					DoLeap(); 
				}
				if ( NextAttack && groundCheck.Hit && !NextLeap && Body.Velocity.z < 10 )
				{
					DoAttack();
				}

				break;

			case ZombieState.Staggered: // STATE IS STAGGERED
				Agent.Stop();
				Agent.UpdatePosition = false;
				Agent.SetAgentPosition( Agent.WorldPosition );
				Body.GravityScale = 1f;

				groundCheck = Scene.Trace.Ray( WorldPosition + Vector3.Up * 10, WorldPosition + Vector3.Down * 35 )
					.Radius( 64 )
					.IgnoreGameObjectHierarchy( GameObject )
					.WithoutTags( "enemy" )
					.Run();
				// DebugOverlay.Trace( groundCheck );

				if (KnockBack && groundCheck.Hit) 
				{
					Agent.UpdatePosition = true;
					CurrentState = ZombieState.Idle;
				}
				break;
		}
	}
	void DoAttack()
	{
		var attackTrace = Scene.Trace.Sphere( 300, WorldPosition, WorldPosition )
						.IgnoreGameObjectHierarchy( GameObject )
						.WithAllTags( "player", "carbody" )
						.Run();

		AttackParticle.WorldTransform = new Transform(Body.WorldPosition, Rotation.FromPitch(0));
		AttackParticle.ResetEmitter();
		Sound.Play( "sounds/falling-game-character.sound", Body.WorldPosition );

		if ( attackTrace.Hit )
		{
			if ( !attackTrace.GameObject.IsValid ) return;

			Log.Info( $"[LEAP] Attack hit: {attackTrace.GameObject.Name}" );
			attackTrace.GameObject.GetComponentInParent<HealthSystem>().Damage( 500 );

			if ( !attackTrace.GameObject.GetComponent<Rigidbody>().IsValid ) return;
			attackTrace.GameObject.GetComponent<Rigidbody>().ApplyImpulse( Vector3.Up * 100000 );

			Sound.Play( "sounds/metal-hit-cartoon.sound", attackTrace.HitPosition );
		}

		NextAttack = LeapCooldown;

	}

	void DoLeap()
	{

		Sound.Play( "sounds/whaa.sound", Body.WorldPosition );

		var playerRb = Player.GetComponent<Rigidbody>();

		// find ground below player to get accurate target position and flight time
		SceneTraceResult findGround = Scene.Trace.Ray( Player.WorldPosition, Player.WorldPosition + Vector3.Down * 5000 )
			.Radius( 32 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "player" )
			.Run();
		DebugOverlay.Sphere(new Sphere(findGround.HitPosition, 32), Color.Black, 3f );

		Vector2 randomOffset = random.VectorInCircle(100);
		
		// === Konvertiere s&box → Unity (X,Y,Z) → (X,Z,Y) ===
		Vector3 zombieUnity = new Vector3( WorldPosition.x, WorldPosition.z, WorldPosition.y );
		Vector3 playerUnity = new Vector3( playerRb.WorldPosition.x, findGround.HitPosition.z, playerRb.WorldPosition.y );
		Vector3 playerVelUnity = new Vector3( playerRb.Velocity.x, playerRb.Velocity.z, playerRb.Velocity.y );

		DebugOverlay.Sphere( new Sphere( new Vector3( playerRb.WorldPosition.x, playerRb.WorldPosition.y, findGround.HitPosition.z  ), 32 ), Color.White, 3f );

		// === Berechne requiredLateralSpeed für feste Flugzeit ===
		Vector3 directionXZ = new Vector3( playerUnity.x - zombieUnity.x, 0f, playerUnity.z - zombieUnity.z );
		float horizontalDistance = directionXZ.Length;
		float requiredLateralSpeed = horizontalDistance / LeapFlightTime;

		// === Nutze solve_ballistic_arc_lateral ===
		if ( Ballistics.solve_ballistic_arc_lateral(
			zombieUnity,
			requiredLateralSpeed,
			playerUnity, // random offset, damit der Leap nicht immer exakt auf den Spieler zielt
			playerVelUnity,
			LeapMaxHeightOffset,
			out Vector3 fireVelUnity,
			out float gravityNeeded,
			out Vector3 impactPointUnity ) )
		{
			// === Konvertiere Unity → s&box (X,Z,Y) → (X,Y,Z) ===
			Vector3 fireVelocity = new Vector3( fireVelUnity.x, fireVelUnity.z, fireVelUnity.y );
			Vector3 impactPoint = new Vector3( impactPointUnity.x, impactPointUnity.z, impactPointUnity.y );

			float worldGravity = Scene.PhysicsWorld.Gravity.Length;
			float gravityScale = Math.Max( 1f, gravityNeeded / worldGravity );

			Log.Info( $"[LEAP] SUCCESS  gravityScale: {gravityScale:F2}" );

			// === Wende Leap an ===
			TargetPos = impactPoint;
			Body.GravityScale = gravityScale;
			Body.Velocity = fireVelocity;
			NextLeap = LeapCooldown;
		}
		else if ( Ballistics.solve_ballistic_arc_lateral(
			zombieUnity,
			requiredLateralSpeed,
			playerUnity,
			LeapMaxHeightOffset,
			out fireVelUnity,
			out gravityNeeded ) )
		{
			// === Konvertiere Unity → s&box (X,Z,Y) → (X,Y,Z) ===
			Vector3 fireVelocity = new Vector3( fireVelUnity.x, fireVelUnity.z, fireVelUnity.y );
			Vector3 impactPoint = new Vector3( playerUnity.x, playerUnity.z, playerUnity.y );

			float worldGravity = Scene.PhysicsWorld.Gravity.Length;
			float gravityScale = Math.Max( 1f, gravityNeeded / worldGravity );

			Log.Info( $"[LEAP] NO PREDICTION gravityScale: {gravityScale:F2}" );

			// === Wende Leap an ===
			TargetPos = impactPoint;
			Body.GravityScale = gravityScale;
			Body.Velocity = fireVelocity;
			NextLeap = LeapCooldown;

		}
	}
}
