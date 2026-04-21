using Sandbox;
using System;
using static Ballistics;
using static HealthSystem;

public enum VampireState { Moving, Attack, Staggered }


public sealed class VampireBrain : Component, HealthSystem.IHealthEvent
{
	GameObject Player;
	Rigidbody PlayerBody;
	[Property] TextRenderer StateDebugText;
	[Property] bool DebugMode;
	[Property] GameObject ParticleObject;
	ParticleEmitter BloodEmitter;
	[Property] GameObject DeathParticle;

	public VampireState CurrentState;

	SceneTraceResult GroundTrace;

	Vector3 CircleOffset;
	Vector3 TargetPosition;
	Vector3 MittelPunkt;
	float SchwebeDistance = 25;
	float SchwebeFrequenz = 4f;

	float AttackCharge = 0;

	TimeUntil NextOffset;
	TimeUntil UntilNextIdleSound;
	Random random;

	void IHealthEvent.OnDeath()
	{
		Sound.Play( "sounds/vampire/vampire-death.sound", GameObject.WorldPosition );
		DeathParticle.Clone( GameObject.WorldPosition + Vector3.Up * 150, WorldRotation, WorldScale * 1.5f );
		GameObject.Parent.Destroy();
	}

	protected override void OnStart()
	{
		random = new Random();
		if ( !DebugMode ) { StateDebugText.Enabled = false; }
		Player = Scene.FindAllWithTag( "carbody" ).First<GameObject>();
		PlayerBody = Player.GetComponent<Rigidbody>();
		BloodEmitter = ParticleObject.GetComponent<ParticleEmitter>();
		// Log.Info(Player.Name);

		CurrentState = VampireState.Moving;

		GroundTrace = Scene.Trace
			.Ray( WorldPosition, WorldPosition + Vector3.Down * 3000 )
			.Radius( 1 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "enemy", "player", "dead" )
			.Run();

		// HorizontalOffset = Player.WorldRotation.Left * random.Int( 1000, 3000 ) * random.Int( -1, 1 );
		CircleOffset = new Vector3(  random.Float( -0.5f, 0.5f ), random.Float( -1, 1 ), 0 ).Normal;
		MittelPunkt = GroundTrace.EndPosition + Vector3.Up * 80;

		SchwebeFrequenz += random.Float( -0.1f, 0.1f );
		GameObject.WorldPosition = MittelPunkt;
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
		Vector3 hoverHeight = (GroundTrace.EndPosition + Vector3.Up * 50).WithY( 0 ).WithX( 0 );
		if ( !GroundTrace.Hit ) { hoverHeight = Vector3.Up * 100; }
		TargetPosition = playerPosition + CircleOffset * 1000 + hoverHeight;

		switch ( CurrentState ) 
		{
			default: 
				StateDebugText.Text = "DEFAULT";
				break;

			// WANDER
			case VampireState.Moving: 
				StateDebugText.Text = "MOVING";
				
				if (UntilNextIdleSound) 
				{
					Sound.Play("sounds/vampire/vampire-idle.sound", WorldPosition);
					UntilNextIdleSound = random.Float(6, 10);
				}

				MittelPunkt = MittelPunkt.LerpTo(TargetPosition, Time.Delta * (TargetPosition - MittelPunkt).Length.Remap(0, 5000, 1, 3));
				
				if ( (playerPosition - WorldPosition).Length < 1300 ) { Attack(); }
				else { BloodEmitter.Enabled = false; }
				
				break;

			// ATTACK
			case VampireState.Attack:
				StateDebugText.Text = "ATTACK";


				break;

			// STAGGERED
			case VampireState.Staggered:
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
		Player.GetComponentInParent<HealthSystem>().Damage(1, false);
		// Particle & Sound
		BloodEmitter.Enabled = true;
		ParticleObject.WorldPosition = PlayerBody.WorldPosition + Vector3.Up * 100;
	}


}
