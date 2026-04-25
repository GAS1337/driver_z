using Sandbox;
using System;
using static Ballistics;
using static HealthSystem;

public enum GhostState { Idle, Moving, Attack, Staggered }


public sealed class GhostBrain : Component, HealthSystem.IHealthEvent
{
	GameObject Player;
	Rigidbody PlayerBody;
	[Property] GameObject GhostBall;
	[Property] TextRenderer StateDebugText;
	[Property] bool DebugMode;
	[Property] SoundEvent AttackSound;
	[Property] SoundEvent IdleSound;
	[Property] SoundEvent DeathSound;

	public GhostState CurrentState;

	SceneTraceResult GroundTrace;

	Vector3 HorizontalOffset;
	Vector3 TargetPosition;
	Vector3 SchwebeMittelPunkt;
	float SchwebeDistance = 100;
	float SchwebeFrequenz = 2f;

	Vector3 IdleKreisPunkt;
	float IdleKreisRadius = 1000f;
	float IdleKreisAngle = 0f;

	TimeSince timeSinceLastAttack;
	float AttackCharge = 0;

	TimeUntil NextOffset;
	Random random;

	void IHealthEvent.OnDeath()
	{
		Sound.Play(DeathSound, WorldPosition);
		GameObject.Parent.Destroy();
	}

	protected override void OnStart()
	{
		random = new Random();

		Player = Scene.FindAllWithTag( "carbody" ).First<GameObject>();
		PlayerBody = Player.GetComponent<Rigidbody>();

		if ( !DebugMode ) { StateDebugText.Enabled = false; }

		CurrentState = GhostState.Moving;

		GroundTrace = Scene.Trace
			.Ray( WorldPosition, WorldPosition + Vector3.Down * 3000 )
			.Radius( 1 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "player", "dead" )
			.Run();

		HorizontalOffset = (Vector3)random.VectorInCircle(1000);
		SchwebeMittelPunkt = GroundTrace.EndPosition + Vector3.Up * 80;

		SchwebeFrequenz += random.Float( -0.1f, 0.1f );
		GameObject.WorldPosition = SchwebeMittelPunkt;

		IdleKreisPunkt = WorldPosition.WithZ( GroundTrace.EndPosition.z ) + (Vector3)random.VectorInCircle( 1 ).Normal * random.Int( 300, 500 );
	}

	protected override void OnFixedUpdate()
	{
		GroundTrace = Scene.Trace
			.Ray( TargetPosition, TargetPosition + Vector3.Down * 3000 )
			.Radius( 1 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "player", "dead" )
			.Run();

		Vector3 playerPosition = Player.WorldPosition;
		Vector3 hoverHeight = (GroundTrace.EndPosition + Vector3.Up * 1000).WithY( 0 ).WithX( 0 );
		if ( !GroundTrace.Hit ) { hoverHeight = Vector3.Up * 1000; }

		switch ( CurrentState ) 
		{
			default: 
				StateDebugText.Text = "DEFAULT";
				break;

			case GhostState.Idle:
				StateDebugText.Text = "IDLE";

				// Wenn Ghost weiter als der Radius * 1.5f vom Punkt entfernt ist soll er einen neuen zufälligen finden
				if ((WorldPosition.WithZ(0) - IdleKreisPunkt.WithZ(0)).Length > IdleKreisRadius * 1.5f)
				{
					IdleKreisPunkt = WorldPosition.WithZ(GroundTrace.EndPosition.z) + (Vector3)random.VectorInCircle(1).Normal * random.Int(300, 500);
				}

				// Winkel un damit Koordinaten vom aufm Kreis berechnen
				IdleKreisAngle += Time.Delta * 0.2f; // * Speed
				float x = MathF.Cos(IdleKreisAngle);
				float y = MathF.Sin(IdleKreisAngle);

				// TargetPosition aus IdleKreisPunkt, Koordinaten und height berechnen, SchwebeMittelPunkt zu TargetPos LERPen
				TargetPosition = IdleKreisPunkt + new Vector3(x, y, 0).Normal * IdleKreisRadius + hoverHeight;
				SchwebeMittelPunkt = SchwebeMittelPunkt.LerpTo(TargetPosition, Time.Delta * (TargetPosition - SchwebeMittelPunkt).Length.Remap(0, 5000, 0.5f, 1));

				if (DebugMode)
				{
					DebugOverlay.Sphere( new Sphere( TargetPosition, 16 ), Color.Red );
					DebugOverlay.Sphere( new Sphere( IdleKreisPunkt.WithZ( GroundTrace.EndPosition.z ), 16 ), Color.Blue );
				}
				
				// Zeit schiebt Sinusfunktion(Welle) voran, multipliziert mit Frequenz für enge oder weite Wellen, 
				// dann mit Distanz multiplizieren und auf MittelPunkt addieren
				GameObject.WorldPosition = SchwebeMittelPunkt + Vector3.Up * (MathF.Sin( Time.Now * (SchwebeFrequenz) ) * SchwebeDistance);

				GameObject.WorldRotation = Rotation.LookAt( TargetPosition - SchwebeMittelPunkt, Vector3.Up );

				if ( (playerPosition - WorldPosition).Length < 10000 ) { CurrentState = GhostState.Moving; }	

				break;

			// MOVING&SHOOTING
			case GhostState.Moving: 
				StateDebugText.Text = "WANDER";
				
				TargetPosition = playerPosition.WithZ(0) + (WorldPosition.WithZ( 0 ) - playerPosition.WithZ(0) ).Normal * 5500 + hoverHeight + HorizontalOffset;
				SchwebeMittelPunkt = SchwebeMittelPunkt.LerpTo(TargetPosition, Time.Delta * (TargetPosition - SchwebeMittelPunkt).Length.Remap(0, 5000, 0.2f, 0.5f));
				// SchwebeMittelPunkt = SchwebeMittelPunkt + ( TargetPosition - SchwebeMittelPunkt ) * 0.5f + Vector3.Left * (MathF.Sin( (TargetPosition - SchwebeMittelPunkt).Length * SchwebeFrequenz ) * SchwebeDistance);
				
				// Zeit schiebt Sinusfunktion(Welle) voran, multipliziert mit Frequenz für enge oder weite Wellen, dann mit Distanz multiplizieren und auf MittelPunkt addieren
				GameObject.WorldPosition = SchwebeMittelPunkt + Vector3.Up * (MathF.Sin( Time.Now * (SchwebeFrequenz) ) * SchwebeDistance);
				
				LookAtPlayer();
				
				if ( timeSinceLastAttack > 4 && playerPosition.Distance(WorldPosition) < 7000 && playerPosition.Distance( WorldPosition ) > 2000 ) 
				{ timeSinceLastAttack = random.Float(0f, 0.2f); Attack(); }
				else {  }
				if ( timeSinceLastAttack < 0.01f )
				{
					// HorizontalOffset = Player.WorldRotation.Left * random.Int( 1, 3 ) * random.Int( -1, 1 );
				}
				// if ( (TargetPosition - MittelPunkt).IsNearlyZero(500) ) CurrentState = GhostState.Attack;
				
				if ( (playerPosition - WorldPosition).Length > 10000 ) { CurrentState = GhostState.Idle; }	
				break;

			// ATTACK
			case GhostState.Attack:
				StateDebugText.Text = "ATTACK";


				break;

			// STAGGERED
			case GhostState.Staggered:
				StateDebugText.Text = "STAGGERED";

				break;
		}


	}

	private void LookAtPlayer()
	{
		if ( GameObject.WorldRotation.Forward.Angle( Player.WorldPosition - GameObject.WorldPosition ) > 1 )
		{
			GameObject.WorldRotation = GameObject.WorldRotation.LerpTo( Rotation.LookAt( Player.WorldPosition - GameObject.WorldPosition, Vector3.Up ), 0.5f );
		}
	}

	private void Attack()
	{
		SceneTraceResult sightlineCheck = Scene.Trace
			.Sphere( 16, WorldPosition, PlayerBody.WorldPosition + Vector3.Up * 100 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "player", "world" )
			.Run();

		if ( sightlineCheck.Hit ) 
		{
			Log.Info( "No line of sight to player, skipping attack" );
			HorizontalOffset = (Vector3)random.VectorInCircle( 1000 );
			return; 
		}

		GameObject newBall = GhostBall.Clone( WorldPosition );
		Rigidbody newBody = newBall.GetComponent<Rigidbody>();
		// newBody.Velocity = Vector3.Down * 100;

		// Sound
		newBall.GetComponent<GhostBallLogic>().attackSoundHandle = Sound.Play( AttackSound, newBall.WorldPosition );

		// Unity Conversion
		Vector3 UnityProjPos = new Vector3( WorldPosition.x, WorldPosition.z, WorldPosition.y );
		Vector3 UnityTargetPos = new Vector3( Player.WorldPosition.x, Player.WorldPosition.z + 100, Player.WorldPosition.y ) + random.VectorInSphere(1);
		Vector3 UnityTargetVel = new Vector3( PlayerBody.Velocity.x, PlayerBody.Velocity.z, PlayerBody.Velocity.y );
		if ( UnityTargetVel.Length < 1 ) { UnityTargetVel = Vector3.Forward; }

		if ( Ballistics.solve_ballistic_arc( UnityProjPos, 
			5000, 
			UnityTargetPos, 
			UnityTargetVel, 
			Scene.PhysicsWorld.Gravity.Length * 0.001f,
			out Vector3 s0, out Vector3 s1 ) > 0 ) 
		{ 
			// Convert back to S&box
			Vector3 solutionVelocity = new Vector3(s0.x, s0.z, s0.y);
			
			//Apply Velocity
			newBody.Velocity = solutionVelocity;
		}
	}


}
