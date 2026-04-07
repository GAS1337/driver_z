using Sandbox;
using System;
using static Ballistics;
using static HealthSystem;

public enum GhostState { Moving, Attack, Staggered }


public sealed class GhostBrain : Component, HealthSystem.IHealthEvent
{
	GameObject Player;
	Rigidbody PlayerBody;
	[Property] GameObject GhostBall;
	[Property] TextRenderer StateDebugText;

	public GhostState CurrentState;

	SceneTraceResult GroundTrace;

	Vector3 HorizontalOffset;
	Vector3 TargetPosition;
	Vector3 MittelPunkt;
	float SchwebeDistance = 100;
	float SchwebeFrequenz = 2f;

	float AttackCharge = 0;

	TimeUntil NextOffset;
	Random random;

	void IHealthEvent.OnDeath()
	{
		GameObject.Parent.Destroy();
	}

	protected override void OnStart()
	{
		random = new Random();
		Player = Scene.FindAllWithTag( "carbody" ).First<GameObject>();
		PlayerBody = Player.GetComponent<Rigidbody>();
		Log.Info(Player.Name);

		CurrentState = GhostState.Moving;

		GroundTrace = Scene.Trace
			.Ray( WorldPosition, WorldPosition + Vector3.Down * 3000 )
			.Radius( 1 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "player", "dead" )
			.Run();

		HorizontalOffset = Player.WorldRotation.Left * random.Int( 1000, 3000 ) * random.Int( -1, 1 );
		MittelPunkt = GroundTrace.EndPosition + Vector3.Up * 80;

		SchwebeFrequenz += random.Float( -0.1f, 0.1f );
		GameObject.WorldPosition = MittelPunkt;
	}

	protected override void OnUpdate()
	{
		GroundTrace = Scene.Trace
			.Ray( TargetPosition, TargetPosition + Vector3.Down * 3000 )
			.Radius( 1 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "player", "dead" )
			.Run();

		Vector3 playerPosition = Player.WorldPosition;
		Vector3 hoverHeight = (GroundTrace.EndPosition + Vector3.Up * 500).WithY( 0 ).WithX( 0 );
		TargetPosition = playerPosition + Player.WorldRotation.Backward.WithZ(0).Normal * 3000 + hoverHeight + HorizontalOffset;

		switch ( CurrentState ) 
		{
			default: 
				StateDebugText.Text = "DEFAULT";
				break;

			// WANDER
			case GhostState.Moving: 
				StateDebugText.Text = "WANDER";
				
				MittelPunkt = MittelPunkt.LerpTo(TargetPosition, Time.Delta * (TargetPosition - MittelPunkt).Length.Remap(0, 5000, 1, 3));
				// MittelPunkt = MittelPunkt + ( TargetPosition - MittelPunkt ) * 0.5f + Vector3.Left * (MathF.Sin( (TargetPosition - MittelPunkt).Length * SchwebeFrequenz ) * SchwebeDistance);

				if ( AttackCharge >= 4 ) { AttackCharge = 0; Attack(); }
				else { AttackCharge += 1 * Time.Delta; }

				if ( AttackCharge == 0 )
				{
					HorizontalOffset = Player.WorldRotation.Left * random.Int( 1000, 3000 ) * random.Int( -1, 1 );
				}
				// if ( (TargetPosition - MittelPunkt).IsNearlyZero(500) ) CurrentState = GhostState.Attack;
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

		// Zeit schiebt Sinusfunktion(Welle) voran, multipliziert mit Frequenz für enge oder weite Wellen, dann mit Distanz multiplizieren und auf MittelPunkt addieren
		GameObject.WorldPosition = MittelPunkt + Vector3.Up * (MathF.Sin( Time.Now * (SchwebeFrequenz) ) * SchwebeDistance);
		LookAtPlayer();
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
		GameObject newBall = GhostBall.Clone( WorldPosition );
		Rigidbody newBody = newBall.GetComponent<Rigidbody>();
		// newBody.Velocity = Vector3.Down * 100;

		// Unity Conversion
		Vector3 UnityProjPos = new Vector3( WorldPosition.x, WorldPosition.z, WorldPosition.y );
		Vector3 UnityTargetPos = new Vector3( Player.WorldPosition.x, Player.WorldPosition.z + 50, Player.WorldPosition.y ) + random.VectorInSphere(200);
		Vector3 UnityTargetVel = new Vector3( PlayerBody.Velocity.x, PlayerBody.Velocity.z, PlayerBody.Velocity.y );

		if ( Ballistics.solve_ballistic_arc( UnityProjPos, 
			3000, 
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
