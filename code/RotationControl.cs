using Sandbox;

public sealed class RotationControl : Component
{
	[Property] Rigidbody CarBody;

	protected override void OnFixedUpdate()
	{
		SceneTraceResult groundCheck = Scene.Trace.Ray( CarBody.WorldPosition + CarBody.WorldRotation.Up * 10, CarBody.WorldPosition + CarBody.WorldRotation.Down * 20)
			.Radius( 1 )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();
		DebugOverlay.Trace( groundCheck );

		if ( groundCheck.Hit )
		{
			CarBody.WorldRotation = CarBody.WorldRotation.Angles().WithRoll( CarBody.WorldRotation.Roll() / 1.5f );
		}
		else 
		{
			if ( Input.Down( "Forward" ) )
			{
				CarBody.AngularVelocity += CarBody.WorldRotation.Right * 0.05f;
			}
		}

	}
}
