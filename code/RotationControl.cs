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

	bool IsGrounded;
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
		Line VelLine = new Line(CarBody.WorldPosition + Vector3.Up * 100, CarBody.WorldPosition + CarBody.Velocity * 0.25f + Vector3.Up * 100);
		// DebugOverlay.Line( VelLine );
		if ( LogSpeed )
		{
			Log.Info( "Speed/4: " + VelLine.Delta.Length );
			LogSpeed =+ 1;
		}

		groundCheck = Scene.Trace.Ray( CarBody.WorldPosition + CarBody.WorldRotation.Up * 10, CarBody.WorldPosition + CarBody.WorldRotation.Down * 35)
			.Radius( 1 )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();
		DebugOverlay.Trace( groundCheck );

		if ( Input.Down( "Forward" ) )
		{
			foreach ( WheelJoint wheel in WheelJoints )
			{
				groundCheck = Scene.Trace.Ray( wheel.WorldPosition + CarBody.WorldRotation.Up * 10, wheel.WorldPosition + CarBody.WorldRotation.Down * 52 ) // 48 is radius
					.Radius( 1 )
					.IgnoreGameObjectHierarchy( GameObject )
					.Run();
				DebugOverlay.Trace( groundCheck );
				if ( groundCheck.Hit ) 
				{ 
					GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) + 1;
					CarBody.ApplyForceAt( wheel.WorldPosition, wheel.WorldRotation.Forward.Cross( groundCheck.Normal ) * 3000000 );
				}
				else { GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) - 1; }
			}
		}
		if ( Input.Down( "Backward" ) )
		{
			foreach ( WheelJoint wheel in WheelJoints )
			{
				groundCheck = Scene.Trace.Ray( wheel.WorldPosition + CarBody.WorldRotation.Up * 10, wheel.WorldPosition + CarBody.WorldRotation.Down * 52 ) // 48 is radius
					.Radius( 1 )
					.IgnoreGameObjectHierarchy( GameObject )
					.Run();
				DebugOverlay.Trace( groundCheck );
				if ( groundCheck.Hit )
				{
					GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) + 1;
					CarBody.ApplyForceAt( wheel.WorldPosition, wheel.WorldRotation.Backward.Cross( groundCheck.Normal ) * 3000000 );
				}
				else { GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) - 1; }
			}
		}
		if ( GroundedWheels > 2 ) { IsGrounded = true; }
		else { IsGrounded = false; }
		Log.Info($"{GroundedWheels} {IsGrounded}");


		if ( IsGrounded )
		{
			// CarBody.AngularDamping = 0;
			// CarBody.WorldRotation = CarBody.WorldRotation.Angles().WithRoll( CarBody.WorldRotation.Roll() / 1.5f );
			Vector3 cheatSteering = Vector3.Zero;
			if ( Input.Down( "Left" ) )
			{
				// Log.Info(CarBody.Velocity.Length);
				// CarBody.AngularVelocity += CarBody.WorldRotation.Up * 0.1f;
				if ( CarBody.Velocity.Length > 360 )
				{
					cheatSteering = (CarBody.WorldRotation.Right) * CarBody.Velocity.Length / 50;
				}
				//CarBody.Velocity += cheatSteering;
				// Rotation rot = Rotation.From( CarBody.WorldRotation.Pitch(), CarBody.WorldRotation.Yaw() + 170, CarBody.WorldRotation.Roll() );
				// CarBody.SmoothRotate(rot, 1f, Time.Delta); 
			}
			else if ( Input.Down( "Right" ) )
			{
				// CarBody.AngularVelocity += CarBody.WorldRotation.Down * 0.1f;
				if ( CarBody.Velocity.Length > 360 ) 
				{
					cheatSteering = (CarBody.WorldRotation.Left) * CarBody.Velocity.Length / 50;
				}
				//CarBody.Velocity += cheatSteering;
				// Rotation rot = Rotation.From( CarBody.WorldRotation.Pitch(), CarBody.WorldRotation.Yaw() - 170, CarBody.WorldRotation.Roll() );
				// CarBody.SmoothRotate( rot, 1f, Time.Delta );
			}
		}
		else
		{
			// CarBody.AngularDamping = 10;

			if ( Input.Down( "Forward" ) )
			{
				// CarBody.AngularVelocity += CarBody.WorldRotation.Right * CarBody.Velocity.Length.Remap( 0, 5000, 0f, 0.3f );
			}

		}

	}
}
