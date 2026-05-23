using Sandbox;
using System;

public sealed class RotationControl : Component
{
	[Property] Rigidbody CarBody;

	[Property] WheelJoint FrontLeft;
	[Property] WheelJoint FrontRight;
	[Property] WheelJoint RearLeft;
	[Property] WheelJoint RearRight;
	List<WheelJoint> WheelJoints;
	List<WheelJoint> LeftWheelJoints;
	List<WheelJoint> RightWheelJoints;

	[Property] float Speed;
	[Property] GameObject Trails;

	public bool IsGrounded;
	int GroundedWheels;
	public SceneTraceResult groundCheck;

	TimeUntil LogSpeed;

	protected override void OnEnabled()
	{
		WheelJoints = new List<WheelJoint>();
		WheelJoints.Add(FrontLeft);
		WheelJoints.Add(FrontRight);
		WheelJoints.Add(RearLeft);
		WheelJoints.Add(RearRight);

		LeftWheelJoints = new List<WheelJoint>();
		LeftWheelJoints.Add( FrontLeft );
		LeftWheelJoints.Add( RearLeft );

		RightWheelJoints = new List<WheelJoint>();
		RightWheelJoints.Add( RearRight );
		RightWheelJoints.Add( FrontRight );
	}

	protected override void OnFixedUpdate()
	{
		Line VelLine = new Line( CarBody.WorldPosition + Vector3.Up * 100, CarBody.WorldPosition + CarBody.Velocity * 0.25f + Vector3.Up * 100 );
		// DebugOverlay.Line( VelLine );
		if ( LogSpeed )
		{
			Log.Info( "Speed/4: " + VelLine.Delta.Length );
			LogSpeed = +1;
		}

		if ( Input.Down( "Forward" ) && Input.Down( "Run" ) )
		{
			foreach ( WheelJoint wheel in WheelJoints )
			{
				groundCheck = CheckGroundForWheel( wheel );

				if ( groundCheck.Hit )
				{
					GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) + 1;
					ApplyForceToWheel( wheel, 1.3f );
				}
				else { GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) - 1; }
			}
		}
		else if ( Input.Down( "Forward" ) )
		{
			foreach ( WheelJoint wheel in WheelJoints )
			{
				groundCheck = CheckGroundForWheel( wheel );

				if ( groundCheck.Hit )
				{
					GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) + 1;
					ApplyForceToWheel( wheel, 1f );
				}
				else 
				{ 
					GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) - 1; 
				}
			}
		}
		else if ( Input.Down( "Backward" ) )
		{
			foreach ( WheelJoint wheel in WheelJoints )
			{
				groundCheck = CheckGroundForWheel( wheel );


				if ( groundCheck.Hit )
				{
					GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) + 1; 
					ApplyForceToWheel( wheel, -1f );
				}
				else { GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) - 1; }
			}
		}
		else
		{
			foreach ( WheelJoint wheel in WheelJoints )
			{
				groundCheck = CheckGroundForWheel( wheel );

				if ( groundCheck.Hit )
				{
					GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) + 1;
				}
				else { GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) - 1; }
			}
		}

		if ( GroundedWheels > 2 ) { IsGrounded = true; }
		else { IsGrounded = false; }
		// Log.Info($"{GroundedWheels} {IsGrounded}");

		if ( IsGrounded )
		{
			Trails.Enabled = true;
			Vector3 cheatSteering = Vector3.Zero;
			if ( Input.Down( "Left" ) )
			{
				if ( CarBody.Velocity.Length > 360 )
				{
					cheatSteering = (CarBody.WorldRotation.Right) * CarBody.Velocity.Length.Remap(0, 3000, 0, 40 );
				}
				CarBody.Velocity += cheatSteering;
			}
			else if ( Input.Down( "Right" ) )
			{
				if ( CarBody.Velocity.Length > 360 )
				{
					cheatSteering = (CarBody.WorldRotation.Left) * CarBody.Velocity.Length.Remap( 0, 3000, 0, 40 );
				}
				CarBody.Velocity += cheatSteering;
			}
		}
		else
		{
			Trails.Enabled = false;
			// Air Control
		}
	}

	private void ApplyForceToWheel( WheelJoint wheel, float factor )
	{
		Vector3 forcePosition = wheel.WorldPosition;
		if ( wheel.Tags.Has( "rear_wheel" ) && GroundedWheels < 4 ) 
		{ 
			forcePosition = CarBody.WorldPosition.WithZ( wheel.WorldPosition.z) + wheel.WorldRotation.Forward.Cross( groundCheck.Normal ) * -50;
			DebugOverlay.Sphere(new Sphere(forcePosition, 20));
			Log.Info(wheel.GameObject.Name);
		}
		else if ( wheel.Tags.Has( "front_wheel" ) && GroundedWheels < 4 )
		{
			forcePosition = CarBody.WorldPosition.WithZ( wheel.WorldPosition.z ) + wheel.WorldRotation.Forward.Cross( groundCheck.Normal ) * 50;
			DebugOverlay.Sphere( new Sphere( forcePosition, 20 ) );
			Log.Info(wheel.GameObject.Name);
		}
		CarBody.ApplyForceAt( forcePosition, wheel.WorldRotation.Forward.Cross( groundCheck.Normal ) * Speed * factor);
		DebugOverlay.Line( new Line( forcePosition, forcePosition + (groundCheck.Normal ) * 500 ) );
	}

	private SceneTraceResult CheckGroundForWheel( WheelJoint wheel )
	{
		// Erstellt eine lokale Drehung um 90 Grad (hier um Pitch/X, falls nötig auf Roll/Y ändern)
		Rotation localCorrection = Rotation.FromPitch( 90 );
		Rotation traceRotation = wheel.WorldRotation * localCorrection;

		groundCheck = Scene.Trace.Cylinder( 55, 55, wheel.WorldPosition, wheel.WorldPosition + Vector3.Down ) // 48 is radius
			.Rotated(traceRotation)
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();
		// DebugOverlay.Trace( groundCheck );
		return groundCheck;
	}
}
