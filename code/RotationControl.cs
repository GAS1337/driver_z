using Sandbox;

public sealed class RotationControl : Component
{
	[Property] Rigidbody CarBody;

	TimeUntil LogSpeed;

	protected override void OnFixedUpdate()
	{
		Line VelLine = new Line(CarBody.WorldPosition + Vector3.Up * 200, CarBody.WorldPosition + CarBody.Velocity * 0.25f + Vector3.Up * 200);
		DebugOverlay.Line( VelLine );
		if ( LogSpeed )
		{
			// Log.Info( "Speed/4: " + VelLine.Delta.Length );
			LogSpeed =+ 1;
		}

		SceneTraceResult groundCheck = Scene.Trace.Ray( CarBody.WorldPosition + CarBody.WorldRotation.Up * 10, CarBody.WorldPosition + CarBody.WorldRotation.Down * 20)
			.Radius( 1 )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();
		// DebugOverlay.Trace( groundCheck );

		if ( groundCheck.Hit )
		{
			// CarBody.WorldRotation = CarBody.WorldRotation.Angles().WithRoll( CarBody.WorldRotation.Roll() / 1.5f );

			if ( Input.Down( "Left" ) )
			{
				// Log.Info(CarBody.Velocity.Length);
				// CarBody.AngularVelocity += CarBody.WorldRotation.Up * 0.1f;
				CarBody.Velocity += (CarBody.WorldRotation.Right) * CarBody.Velocity.Length.Remap(0, 5000, 0, 40 );
				Rotation rot = Rotation.From( CarBody.WorldRotation.Pitch(), CarBody.WorldRotation.Yaw() + 170, CarBody.WorldRotation.Roll() );
				// CarBody.SmoothRotate(rot, 1f, Time.Delta); 
			}
			else if ( Input.Down( "Right" ) )
			{
				// CarBody.AngularVelocity += CarBody.WorldRotation.Down * 0.1f;
				CarBody.Velocity += (CarBody.WorldRotation.Left) * CarBody.Velocity.Length.Remap( 0, 5000, 0, 40 );
				Rotation rot = Rotation.From( CarBody.WorldRotation.Pitch(), CarBody.WorldRotation.Yaw() - 170, CarBody.WorldRotation.Roll() );
				// CarBody.SmoothRotate( rot, 1f, Time.Delta );
			}
		}
		else 
		{
			// CarBody.AngularVelocity = CarBody.AngularVelocity.WithFriction( 0.01f );

			if ( Input.Down( "Forward" ) )
			{
				CarBody.AngularVelocity += CarBody.WorldRotation.Right * CarBody.Velocity.Length.Remap( 0, 5000, 0f, 0.3f );
			}

		}

	}
}
