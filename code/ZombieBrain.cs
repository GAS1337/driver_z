using Sandbox;
using Sandbox.Navigation;
using System;
using System.Threading.Tasks;
using static Ballistics;
using static HealthSystem;

public enum ZombieState { Wander, Approach, Leap, Slam, Staggered, Culled }

public sealed class ZombieBrain : Component, HealthSystem.IHealthEvent
{
	[Property] NavMeshAgent Agent;
	GameObject Player;
	[Property] Rigidbody Body;
	[Property] ModelRenderer Model;
	[Property] TextRenderer StateDebugText;
	[Property] bool DebugMode;

	[Property] GameObject DeadZombie;
	[Property] GameObject BloodDecal;
	[Property] ParticleRingEmitter AttackParticle;
	[Property] ParticleEffect AttackEffect;

	[Property] float MoveCooldown = 1f;
	float WanderCooldown = 5f;
	[Property] float LeapCooldown = 5f;
	[Property] float SlamCooldown = 3f;
	[Property] float SlamRadius = 350f;

	[Property] int ApproachRange = 7000;
	[Property] int CulledRange = 15000;
	[Property] int LeapRange = 2000;
	[Property] int SlamRange = 1000;
	int RangeOffset;


	// --- Leap-Parameter zum Tunen ---
	[Property] float LeapFlightTime = 1.5f;         // Konstante Flugzeit in Sekunden
	[Property] float LeapMaxHeightOffset = 800f;    // Wie viel höher der Bogen gehen soll

	float DistanceToPlayer;
	Vector3 TargetPos;

	public ZombieState CurrentState;

	TimeUntil NextMove;
	TimeUntil NextWander;
	TimeUntil NextLeap;
	TimeUntil NextSlam;
	TimeUntil NextChargeSlam;
	public TimeUntil KnockBack;

	bool IsChargingSlam;
	TimeSince SinceLeapAnimationStart;
	TimeSince SinceSlamAnimationStart;
	TimeSince SinceHitAnimationStart;
	Vector3 originalScale;

	Random random = new Random();

	SceneTraceResult groundCheck;

	NavMesh NavMesh;

	protected override void OnStart()
	{
		CurrentState = ZombieState.Wander;
		Agent.MoveTo( Body.WorldPosition );
		Player = Scene.FindAllWithTag("carbody").First<GameObject>();

		originalScale = WorldScale;
		RangeOffset = random.Int( -500, 500 );
		LeapRange += RangeOffset;

		if ( !DebugMode ) { StateDebugText.Enabled = false; }
		// Log.Info( $"[START] Player found: {Player.Name}" );
	}

	void IHealthEvent.OnDeath()
	{
		groundCheck = Scene.Trace.Ray( WorldPosition, WorldPosition + Vector3.Down * 3500 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "dead", "player", "playerclip" )
			.Run();
		GameObject newDecal = BloodDecal.Clone( groundCheck.EndPosition, Rotation.LookAt( groundCheck.Normal, Vector3.Up ).Angles().WithYaw(random.Int(-180, 180)) );		
		GameObject _deadClone = DeadZombie.Clone( WorldPosition, WorldRotation, originalScale );
		foreach (GameObject child in _deadClone.Children )
		{
			child.GetComponent<Rigidbody>().Velocity = Body.Velocity;
			child.GetComponent<Rigidbody>().ApplyImpulse( (child.WorldPosition - GameObject.WorldPosition).Normal * 1000 );
			child.GetComponent<Rigidbody>().AngularVelocity = random.VectorInSphere( random.Float( 3, 5) );
			// child.Enabled = random.NextDouble() >= 0.5;
		}

		Sound.Play( "sounds/zombie-dies.sound", _deadClone.WorldPosition );
	}

	protected override void OnFixedUpdate()
	{

		// DebugOverlay.Sphere( new Sphere( Body.WorldPosition, SlamRadius ), Color.Orange );
		DistanceToPlayer = (Player.WorldPosition - WorldPosition).Length;

		switch ( CurrentState ) 
		{
			default:
				Agent.Stop();
				StateDebugText.Text = "Default";
				break;

			case ZombieState.Culled: // STATE IS CULLED
				StateDebugText.Text = "Culled";
				Agent.Stop();
				Agent.Enabled = false;
				// DebugOverlay.Sphere(new Sphere(WorldPosition, 15000));

				if ( DistanceToPlayer < CulledRange ) 
				{
					Agent.Enabled = true;
					Agent.MoveTo( Body.WorldPosition );
					CurrentState = ZombieState.Wander;
				}
				break;

			case ZombieState.Wander: // STATE IS WANDER
				StateDebugText.Text = "Wander";
				Agent.MaxSpeed = 500;
				Agent.Acceleration = 500;

				if ( NextWander && Agent.TargetPosition.HasValue && ((Vector3)Agent.TargetPosition - Body.WorldPosition).IsNearlyZero(300) )
				{
					DoWander();
					NextWander = WanderCooldown + random.Float(0f, 0.1f);
				}

				if ( DistanceToPlayer > CulledRange ) { CurrentState = ZombieState.Culled; }

				if ( DistanceToPlayer < ApproachRange ) { CurrentState = ZombieState.Approach; }
				break;

			case ZombieState.Approach: // STATE IS APPROACH
				StateDebugText.Text = "Approach";

				if ( !(Agent.AgentPosition - Body.WorldPosition).IsNearlyZero(50f) ) { Agent.SetAgentPosition( Body.WorldPosition ); }
				Agent.UpdatePosition = true; Agent.UpdateRotation = false;
				Agent.MaxSpeed = 800;
				Agent.Acceleration = 500;

				// Walk to Player and rotate
				if ( NextMove )
				{
					Agent.MoveTo( Player.WorldPosition + (Body.WorldPosition - Player.WorldPosition).Normal * 200 );
					NextMove = MoveCooldown + random.Float(0f, 0.1f);
				}
				LookAtPlayer();

				// Go to wander if player is far
				if ( DistanceToPlayer > ApproachRange )
				{
					Agent.UpdateRotation = true;
					Agent.MaxSpeed = 240; 
					CurrentState = ZombieState.Wander; 
				}

				// Go to slam if near and NextSlam
				if ( DistanceToPlayer < (SlamRadius * 2) && NextSlam ) { NextChargeSlam = 1; CurrentState = ZombieState.Slam; }

				// Go to Leap if NextMove, Distance und sightlineCheck 
				if ( DistanceToPlayer < LeapRange && DistanceToPlayer > SlamRadius * 3 && NextLeap && NextSlam ) 
				{
					SceneTraceResult checkSightline = Scene.Trace.Sphere( 64, Body.WorldPosition + Vector3.Up * 300, Player.WorldPosition + Vector3.Up * 300 )
						.IgnoreGameObjectHierarchy( GameObject )
						.WithoutTags( "enemy", "player", "world" )
						.Run();
					// DebugOverlay.Trace( checkSightline );
					if ( !checkSightline.Hit ) CurrentState = ZombieState.Leap; 
				}

				break;

			case ZombieState.Slam: // STATE IS SLAM
				StateDebugText.Text = "Slam";
				Agent.Stop();
				LookAtPlayer();
				if ( !IsChargingSlam ) AnimateSlam();
				// Increase Charge to 100 then DoSlam()
				if ( NextChargeSlam ) { IsChargingSlam = false; DoSlam(); }
				
				if ( DistanceToPlayer < 7000 && NextChargeSlam ) { CurrentState = ZombieState.Approach; }

				break;

			case ZombieState.Leap: // STATE IS LEAP

				StateDebugText.Text = "Leap";

				Agent.Stop();
				Agent.UpdatePosition = false; Agent.UpdateRotation = false;

				groundCheck = Scene.Trace.Ray( WorldPosition + Vector3.Up * 10, WorldPosition + Vector3.Down * 5 )
					.Radius( 100 )
					.IgnoreGameObjectHierarchy( GameObject )
					.WithoutTags( "enemy" )
					.Run();
				// DebugOverlay.Trace( groundCheck );

				if (groundCheck.Hit)
				{
					LookAtPlayer();
				}
				else
				{
					Body.SmoothRotate( Rotation.LookAt( Body.WorldPosition + Body.WorldRotation.Forward * 10, Vector3.Up ), 0.5f, 0.01f );
				}

				// Einmal am anfang leapen
				if ( NextLeap ) 
				{ 
					DoLeap();
					AnimateLeap();
				}

				// wenn er wieder aufkommt slammen und zu approach
				if ( NextSlam && groundCheck.Hit && !NextLeap && Body.Velocity.z < 10 )
				{
					DoSlam();

					Body.GravityScale = 1f;
					CurrentState = ZombieState.Approach;
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
					CurrentState = ZombieState.Wander;
				}
				break;
		}
	}

	private void LookAtPlayer()
	{
		if ( Body.WorldRotation.Forward.Angle( Player.WorldPosition - GameObject.WorldPosition ) > 1 )
		{
			Body.SmoothRotate( Rotation.LookAt( Player.WorldPosition - GameObject.WorldPosition, Vector3.Up ).Angles().WithPitch(0), 0.5f, 0.01f );
		}
	}

	void DoWander() 
	{ 
		Vector3 possibleTargetPos = Body.WorldPosition + (Vector3)random.VectorInCircle(1000);
		SceneTraceResult wanderTrace = Scene.Trace.Sphere( 100, possibleTargetPos, possibleTargetPos )
			.Radius( 100 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "player", "world" )
			.Run();

		if ( !wanderTrace.Hit )
		{
			Agent.MoveTo( possibleTargetPos );
		}
		else
		{
			int tries = 0;
			while ( wanderTrace.Hit && tries < 10 )
			{
				possibleTargetPos = Body.WorldPosition + (Vector3)random.VectorInCircle( 1000 );

				wanderTrace = Scene.Trace.Sphere( 300, possibleTargetPos, possibleTargetPos )
					.Radius( 300 )
					.IgnoreGameObjectHierarchy( GameObject )
					.WithoutTags( "enemy", "player", "world" )
					.Run();
				tries++;

				// DebugOverlay.Trace( wanderTrace );
			}
			if ( !wanderTrace.Hit )
			{
				Agent.MoveTo( possibleTargetPos );
			}
		}
	}
	
	void DoSlam()
	{
		AnimateHit();
		Log.Info( $"{GameObject.Name} is slamming!" );
		// Attack Trace
		// AnimateSlam();
		var attackTrace = Scene.Trace.Sphere( SlamRadius, Body.WorldPosition, Body.WorldPosition )
						.IgnoreGameObjectHierarchy( GameObject )
						.WithAllTags( "player", "carbody" )
						.Run();
		// DebugOverlay.Trace(attackTrace);

		// Partikel, Sound
		AttackParticle.WorldTransform = new Transform(Body.WorldPosition, Rotation.FromPitch(0));
		AttackParticle.ResetEmitter();
		Sound.Play( "sounds/falling-game-character.sound", Body.WorldPosition );

		// Wenn er den player trifft
		if ( attackTrace.Hit )
		{
			if ( !attackTrace.GameObject.IsValid ) return;

			Log.Info( $"Slam hit: {attackTrace.GameObject.Name}" );
			attackTrace.GameObject.GetComponentInParent<HealthSystem>().Damage( 500 );

			if ( !attackTrace.GameObject.GetComponent<Rigidbody>().IsValid ) return;
			if (Player.GetComponent<Rigidbody>().Velocity.z < 100 ) 
			{ 
				attackTrace.GameObject.GetComponent<Rigidbody>().ApplyImpulse( Vector3.Up * 50000 + (Player.WorldPosition.WithZ(0) - Body.WorldPosition.WithZ(0)).Normal * 300000 );
				attackTrace.GameObject.GetComponent<Rigidbody>().AngularVelocity 
					= attackTrace.GameObject.WorldRotation.Forward * attackTrace.GameObject.GetComponent<Rigidbody>().Mass * 5 * attackTrace.GameObject.WorldRotation.Right.Dot( ( Player.WorldPosition - Body.WorldPosition ).Normal);
			}
		}

		NextSlam = SlamCooldown;

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
		// DebugOverlay.Sphere(new Sphere(findGround.HitPosition, 32), Color.Black, 3f );

		Vector2 randomOffset = random.VectorInCircle() * random.Int(200, 300);
		
		// === Konvertiere s&box → Unity (X,Y,Z) → (X,Z,Y) ===
		Vector3 zombieUnity = new Vector3( WorldPosition.x, WorldPosition.z, WorldPosition.y );
		Vector3 playerUnity = new Vector3( playerRb.WorldPosition.x + randomOffset.x, findGround.HitPosition.z, playerRb.WorldPosition.y + randomOffset.y );
		Vector3 playerVelUnity = new Vector3( playerRb.Velocity.x, 0, playerRb.Velocity.y );

		// DebugOverlay.Sphere( new Sphere( new Vector3( playerRb.WorldPosition.x, playerRb.WorldPosition.y, findGround.HitPosition.z  ), 32 ), Color.White, 3f );

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
			float gravityScale = Math.Max( 0.5f, gravityNeeded / worldGravity );

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
			float gravityScale = Math.Max( 0.5f, gravityNeeded / worldGravity );

			Log.Info( $"[LEAP] NO PREDICTION gravityScale: {gravityScale:F2}" );

			// === Wende Leap an ===
			TargetPos = impactPoint;
			Body.GravityScale = gravityScale;
			Body.Velocity = fireVelocity;
			NextLeap = LeapCooldown;

		}
	}

	async Task AnimateLeap()
	{
		float duration = LeapFlightTime;
		SinceLeapAnimationStart = 0;

		while ( SinceLeapAnimationStart < duration * 0.5f )
		{
			if ( CurrentState != ZombieState.Leap ) { break; }

			Model.WorldScale = Model.WorldScale.WithZ( Model.WorldScale.z.LerpTo( originalScale.z + 0.2f, 0.01f ) );
			await Task.FrameEnd();
		}
		while ( !groundCheck.Hit )
		{
			if ( CurrentState != ZombieState.Leap ) { break; }

			Model.WorldScale = Model.WorldScale.WithZ( Model.WorldScale.z.LerpTo( originalScale.z - 0.5f, 0.01f ) );
			await Task.FrameEnd();
		}

		if ( !this.IsValid() ) return;
		Model.WorldScale = originalScale;
	}

	async Task AnimateSlam()
	{
		float duration = 1f;
		SinceSlamAnimationStart = 0;
		IsChargingSlam = true;

		while ( SinceSlamAnimationStart < duration * 0.9f )
		{
			if ( CurrentState != ZombieState.Slam ) { IsChargingSlam = false; break; }

			Model.WorldScale = Model.WorldScale.WithZ( Model.WorldScale.z.LerpTo( originalScale.z + 0.2f, 0.01f ) );
			await Task.FrameEnd();
		}
		SinceSlamAnimationStart = 0;
		while ( SinceSlamAnimationStart < duration * 0.1f )
		{
			if ( CurrentState != ZombieState.Slam ) { IsChargingSlam = false; break; }

			Model.WorldScale = Model.WorldScale.WithZ( Model.WorldScale.z.LerpTo( originalScale.z - 0.5f, 0.1f ) );
			await Task.FrameEnd();
		}

		if ( !this.IsValid() ) return;
		Model.WorldScale = originalScale;
	}

	// Nur für ModelRenderer muss noch gemacht werden
	public async Task AnimateHit() 
	{
		float duration = 0.1f;
		SinceHitAnimationStart = 0;

		Model.WorldScale = Model.WorldScale.WithZ( originalScale.z * (1f - 0.15f) );

		while (SinceHitAnimationStart < duration) 
		{ 
			Model.WorldScale = Model.WorldScale.WithZ(Model.WorldScale.z.LerpTo(originalScale.z, 0.1f));
			await Task.FrameEnd();
		}

		if ( !this.IsValid() ) return;
		Model.WorldScale = originalScale;
	}

}
