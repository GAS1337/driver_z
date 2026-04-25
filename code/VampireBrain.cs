using Sandbox;
using System;
using static Ballistics;
using static HealthSystem;

public enum VampireState { Idle, Moving, Attack, Staggered }


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
	public Vector3 TargetPosition;
	Vector3 SchwebeMittelPunkt;
	float SchwebeDistance = 25;
	float SchwebeFrequenz = 4f;

	Vector3 IdleKreisPunkt;
	float IdleKreisRadius = 1000f;
	float IdleKreisAngle = 0f;

	float AttackCharge = 0;

	public TimeUntil UntilKnockBack;
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
		Vector3 hoverHeight = (GroundTrace.EndPosition + Vector3.Up * 50).WithY( 0 ).WithX( 0 );
		if ( !GroundTrace.Hit ) { hoverHeight = Vector3.Up * 100; }

		switch ( CurrentState ) 
		{
			default: 
				StateDebugText.Text = "DEFAULT";
				break;

			// IDLE
			case VampireState.Idle:
				StateDebugText.Text = "IDLE";
				
				if (UntilNextIdleSound) 
				{
					Sound.Play("sounds/vampire/vampire-idle.sound", WorldPosition);
					UntilNextIdleSound = random.Float(6, 10);
				}

				// Wenn Vampir weiter als der Radius * 1.5f vom Punkt entfernt ist soll er einen neuen zufälligen finden
				if ((WorldPosition.WithZ( 0 ) - IdleKreisPunkt.WithZ( 0 )).Length > IdleKreisRadius * 1.5f )
				{
					IdleKreisPunkt = WorldPosition.WithZ(GroundTrace.EndPosition.z) + (Vector3)random.VectorInCircle(1).Normal * random.Int(300, 500);
				}

				// Winkel un damit Koordinaten vom aufm Kreis berechnen
				IdleKreisAngle += Time.Delta * 0.5f; // * speed
				float x = MathF.Cos(IdleKreisAngle);
				float y = MathF.Sin(IdleKreisAngle);

				// TargetPosition aus IdleKreisPunkt, Koordinaten und height berechnen, SchwebeMittelPunkt zu TargetPos LERPen
				TargetPosition = IdleKreisPunkt + new Vector3(x, y, 0).Normal * IdleKreisRadius + hoverHeight;
				SchwebeMittelPunkt = SchwebeMittelPunkt.LerpTo(TargetPosition, Time.Delta * (TargetPosition - SchwebeMittelPunkt).Length.Remap(0, 5000, 1, 3));
				
				if ( DebugMode )
				{
					DebugOverlay.Sphere( new Sphere( TargetPosition, 16 ), Color.Red );
					DebugOverlay.Sphere( new Sphere( IdleKreisPunkt.WithZ( GroundTrace.EndPosition.z ), 16 ), Color.Blue );
				}
				
				// Zeit schiebt Sinusfunktion(Welle) voran, multipliziert mit Frequenz für enge oder weite Wellen, 
				// dann mit Distanz multiplizieren und auf MittelPunkt addieren
				GameObject.WorldPosition = SchwebeMittelPunkt + Vector3.Up * (MathF.Sin( Time.Now * (SchwebeFrequenz) ) * SchwebeDistance);

				GameObject.WorldRotation = Rotation.LookAt( TargetPosition - GameObject.WorldPosition, Vector3.Up );

				if ( (playerPosition - WorldPosition).Length < 10000 ) { CurrentState = VampireState.Moving; }				
				break;


			// WANDER
			case VampireState.Moving: 
				StateDebugText.Text = "MOVING";
				
				if (UntilNextIdleSound) 
				{
					Sound.Play("sounds/vampire/vampire-idle.sound", WorldPosition);
					UntilNextIdleSound = random.Float(6, 10);
				}

				// TargetPosition bestimmen und MittePunkt hinLERPen
				TargetPosition = playerPosition + CircleOffset * 1000 + hoverHeight;
				SchwebeMittelPunkt = SchwebeMittelPunkt.LerpTo(TargetPosition, Time.Delta * (TargetPosition - SchwebeMittelPunkt).Length.Remap(0, 5000, 1, 3));
				// Zeit schiebt Sinusfunktion(Welle) voran, multipliziert mit Frequenz für enge oder weite Wellen, 
				// dann mit Distanz multiplizieren und auf MittelPunkt addieren
				GameObject.WorldPosition = SchwebeMittelPunkt + Vector3.Up * (MathF.Sin( Time.Now * (SchwebeFrequenz) ) * SchwebeDistance);

				LookAtPlayer();

				if ( (playerPosition - WorldPosition).Length < 1300 ) { Attack(); }
				else { BloodEmitter.Enabled = false; }
				
				if ( (playerPosition - WorldPosition).Length > 10000 ) { CurrentState = VampireState.Idle; }
				break;

			// ATTACK
			case VampireState.Attack:
				StateDebugText.Text = "ATTACK";


				break;

			// STAGGERED
			case VampireState.Staggered:
				StateDebugText.Text = "STAGGERED";
				
				BloodEmitter.Enabled = false;

				SchwebeMittelPunkt = SchwebeMittelPunkt.LerpTo( TargetPosition, Time.Delta * (TargetPosition - SchwebeMittelPunkt).Length.Remap( 0, 5000, 1, 3 ) );
				GameObject.WorldPosition = SchwebeMittelPunkt + Vector3.Up * (MathF.Sin( Time.Now * (SchwebeFrequenz) ) * SchwebeDistance);

				if (UntilKnockBack) { CurrentState = VampireState.Moving; }
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
		Player.GetComponentInParent<HealthSystem>().Damage(1, false);
		// Particle & Sound
		BloodEmitter.Enabled = true;
		ParticleObject.WorldPosition = PlayerBody.WorldPosition + Vector3.Up * 100;
	}


}
