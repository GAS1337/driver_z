using Sandbox;
using System;

public sealed class RotationControl : Component
{
	[Property] Rigidbody CarBody;

	[Property] WheelJoint FrontLeft;
	[Property] WheelJoint FrontRight;
	[Property] WheelJoint RearLeft;
	[Property] WheelJoint RearRight;

	public SceneTraceResult groundCheck;

	TimeUntil LogSpeed;

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

		if ( groundCheck.Hit )
		{
			// CarBody.WorldRotation = CarBody.WorldRotation.Angles().WithRoll( CarBody.WorldRotation.Roll() / 1.5f );
			Vector3 cheatSteering;
			if ( Input.Down( "Left" ) )
			{
				// Log.Info(CarBody.Velocity.Length);
				// CarBody.AngularVelocity += CarBody.WorldRotation.Up * 0.1f;
				cheatSteering = (CarBody.WorldRotation.Right) * RearLeft.SpinSpeed.Remap( 0, 6000, 0, 40 );
				if ( RearLeft.SpinSpeed > 360 )
				{
					cheatSteering = (CarBody.WorldRotation.Right) * (RearLeft.SpinSpeed.Remap( 0, 6000, 0, 40 ) + FrontLeft.TargetSteeringAngle);
				}
				CarBody.Velocity += cheatSteering;
				// Rotation rot = Rotation.From( CarBody.WorldRotation.Pitch(), CarBody.WorldRotation.Yaw() + 170, CarBody.WorldRotation.Roll() );
				// CarBody.SmoothRotate(rot, 1f, Time.Delta); 
			}
			else if ( Input.Down( "Right" ) )
			{
				// CarBody.AngularVelocity += CarBody.WorldRotation.Down * 0.1f;
				cheatSteering = (CarBody.WorldRotation.Left) * RearRight.SpinSpeed.Remap( 0, 6000, 0, 40 );
				if (RearRight.SpinSpeed > 360 ) 
				{
					cheatSteering = (CarBody.WorldRotation.Left) * (RearRight.SpinSpeed.Remap( 0, 6000, 0, 40 ) + -FrontRight.TargetSteeringAngle);
				}
				CarBody.Velocity += cheatSteering;
				// Rotation rot = Rotation.From( CarBody.WorldRotation.Pitch(), CarBody.WorldRotation.Yaw() - 170, CarBody.WorldRotation.Roll() );
				// CarBody.SmoothRotate( rot, 1f, Time.Delta );
			}
		}
		else
		{
			CarBody.AngularVelocity = CarBody.AngularVelocity.ClampLength(1);

			if ( Input.Down( "Forward" ) )
			{
				// CarBody.AngularVelocity += CarBody.WorldRotation.Right * CarBody.Velocity.Length.Remap( 0, 5000, 0f, 0.3f );
			}

		}

	}
}
