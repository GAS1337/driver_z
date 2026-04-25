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
			// Log.Info( "Speed/4: " + VelLine.Delta.Length );
			LogSpeed = +1;
		}

		if ( Input.Down( "Forward" ) && Input.Down( "Run" ) )
		{
			foreach ( WheelJoint wheel in WheelJoints )
			{
				groundCheck = Scene.Trace.Ray( wheel.WorldPosition + CarBody.WorldRotation.Up * 10, wheel.WorldPosition + CarBody.WorldRotation.Down * 48 ) // 48 is radius
					.Radius( 10 )
					.IgnoreGameObjectHierarchy( GameObject )
					.Run();
				// DebugOverlay.Trace( groundCheck );
				if ( groundCheck.Hit )
				{
					GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) + 1;
					CarBody.ApplyForceAt( wheel.WorldPosition, wheel.WorldRotation.Forward.Cross( groundCheck.Normal ) * Speed * 1.5f );
				}
				else { GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) - 1; }
			}
		}
		else if ( Input.Down( "Forward" ) )
		{
			foreach ( WheelJoint wheel in WheelJoints )
			{
				groundCheck = Scene.Trace.Ray( wheel.WorldPosition + CarBody.WorldRotation.Up * 10, wheel.WorldPosition + CarBody.WorldRotation.Down * 48 ) // 48 is radius
					.Radius( 10 )
					.IgnoreGameObjectHierarchy( GameObject )
					.Run();
				// DebugOverlay.Trace( groundCheck );
				if ( groundCheck.Hit )
				{
					GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) + 1;

					CarBody.ApplyForceAt( wheel.WorldPosition, wheel.WorldRotation.Forward.Cross( groundCheck.Normal ) * Speed );
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
				groundCheck = Scene.Trace.Ray( wheel.WorldPosition + CarBody.WorldRotation.Up * 10, wheel.WorldPosition + CarBody.WorldRotation.Down * 48 ) // 48 is radius
					.Radius( 10 )
					.IgnoreGameObjectHierarchy( GameObject )
					.Run();
				// DebugOverlay.Trace( groundCheck );

				if ( groundCheck.Hit )
				{
					GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) + 1;
					CarBody.ApplyForceAt( wheel.WorldPosition, wheel.WorldRotation.Backward.Cross( groundCheck.Normal ) * Speed );
				}
				else { GroundedWheels = GroundedWheels.Clamp<int>( 1, 3 ) - 1; }
			}
		}

		if ( GroundedWheels > 2 ) { IsGrounded = true;
									}
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
					cheatSteering = (CarBody.WorldRotation.Right) * CarBody.Velocity.Length.Remap(0, 4000, 0, 40 );
				}
				CarBody.Velocity += cheatSteering;
			}
			else if ( Input.Down( "Right" ) )
			{
				if ( CarBody.Velocity.Length > 360 )
				{
					cheatSteering = (CarBody.WorldRotation.Left) * CarBody.Velocity.Length.Remap( 0, 4000, 0, 40 );
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
}
