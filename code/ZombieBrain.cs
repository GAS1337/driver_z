using Sandbox;
using System;
using static Ballistics;
using static HealthSystem;

public enum ZombieState { Idle, Approach, Leap, Staggered }

public sealed class ZombieBrain : Component, HealthSystem.IHealthEvent
{
	[Property] NavMeshAgent Agent;
	[Property] GameObject Player;
	[Property] Rigidbody Body;
	[Property] TextRenderer StateDebugText;
	[Property] ParticleRingEmitter AttackParticle;
	[Property] ParticleEffect AttackEffect;
	[Property] float ApproachCooldown = 1f;
	[Property] float LeapCooldown = 5f;
	
	// --- Leap-Parameter zum Tunen ---
	[Property] float LeapFlightTime = 1.5f;         // Konstante Flugzeit in Sekunden
	[Property] float LeapMaxHeightOffset = 800f;    // Wie viel höher der Bogen gehen soll

	float DistanceToPlayer;

	public ZombieState CurrentState;

	TimeUntil NextApproach;
	TimeUntil NextLeap;
	TimeUntil NextAttack;
	public TimeUntil KnockBack;

	SceneTraceResult groundCheck;

	protected override void OnStart()
	{
		CurrentState = ZombieState.Idle;
	}

	void HealthSystem.IHealthEvent.OnDeath() 
	{
		GameObject.Destroy();
	}

	protected override void OnFixedUpdate()
	{
		DistanceToPlayer = (Player.WorldPosition - WorldPosition).Length;

		switch ( CurrentState ) 
		{
			default:
				Agent.Stop();
				break;
			case ZombieState.Idle:
				StateDebugText.Text = "Idle";
				Agent.Stop();
				if ( DistanceToPlayer < 7000 ) { CurrentState = ZombieState.Approach; }
				break;

			case ZombieState.Approach:
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

			case ZombieState.Leap:
				groundCheck = Scene.Trace.Ray( WorldPosition + Vector3.Up * 10, WorldPosition + Vector3.Down * 35 )
					.Radius( 48 )
					.IgnoreGameObjectHierarchy( GameObject )
					.WithoutTags( "enemy" )
					.Run();
				DebugOverlay.Trace( groundCheck );

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

				if ( Body.WorldRotation.Forward.Angle( Player.WorldPosition - GameObject.WorldPosition ) > 1 )
				{
					Body.SmoothRotate( Rotation.LookAt( Player.WorldPosition - GameObject.WorldPosition, Vector3.Up ), 0.5f, 0.01f );
				}

				Agent.SetAgentPosition( Agent.WorldPosition );
				Agent.Stop();
				Agent.UpdatePosition = false;

				if ( NextAttack && groundCheck.Hit )
				{
					var attackTrace = Scene.Trace.Sphere( 500, WorldPosition, WorldPosition )
						.IgnoreGameObjectHierarchy( GameObject )
						.WithAllTags( "player", "carbody" )
						.Run();

					AttackParticle.ResetEmitter();

					if ( attackTrace.Hit )
					{
						Log.Info( $"[LEAP] Attack hit: {attackTrace.GameObject.Name}" );
						attackTrace.GameObject.GetComponentInParent<HealthSystem>().Damage( 500 );
						attackTrace.GameObject.GetComponent<Rigidbody>().ApplyImpulse( Vector3.Up * 100000 );
					}

						DebugOverlay.Trace( attackTrace );

					NextAttack = NextLeap + 0.1f; // Attack kurz vor Ende des Leaps, damit er nicht direkt nach der Landung wieder angreift
				}

				// === Leap-Berechnung ===
				if ( !NextLeap ) { break; }
					var playerRb = Player.GetComponent<Rigidbody>();

					// === Konvertiere s&box → Unity (X,Y,Z) → (X,Z,Y) ===
					Vector3 zombieUnity = new Vector3( WorldPosition.x, WorldPosition.z, WorldPosition.y );
					Vector3 playerUnity = new Vector3( Player.WorldPosition.x, Player.WorldPosition.z, Player.WorldPosition.y );
					Vector3 playerVelUnity = new Vector3( playerRb.Velocity.x, playerRb.Velocity.z, playerRb.Velocity.y );

					// === Berechne requiredLateralSpeed für feste Flugzeit ===
					Vector3 directionXZ = new Vector3( playerUnity.x - zombieUnity.x, 0f, playerUnity.z - zombieUnity.z );
					float horizontalDistance = directionXZ.Length;
					float requiredLateralSpeed = horizontalDistance / LeapFlightTime;

					// === Nutze solve_ballistic_arc_lateral ===
					if ( Ballistics.solve_ballistic_arc_lateral(
						zombieUnity,
						requiredLateralSpeed,
						playerUnity,
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
						float gravityScale = Math.Max(1f, gravityNeeded / worldGravity);

						Log.Info( $"[LEAP] SUCCESS - fireVel: {fireVelocity}, gravityScale: {gravityScale:F2}, impact: {impactPoint}" );

						// === Wende Leap an ===
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
					float gravityScale = gravityNeeded / worldGravity;

					Log.Info( $"[LEAP] NO PREDICTION - fireVel: {fireVelocity}, gravityScale: {gravityScale:F2}, impact: {impactPoint}" );

					// === Wende Leap an ===
					Body.GravityScale = gravityScale;
					Body.Velocity = fireVelocity;
					NextLeap = LeapCooldown;
				}

				break;

			case ZombieState.Staggered:
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
}
