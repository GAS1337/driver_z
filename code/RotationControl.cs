using Sandbox;

public sealed class RotationControl : Component
{
	[Property] Rigidbody CarBody;

	protected override void OnFixedUpdate()
	{
		Angles angles = new Angles( CarBody.WorldRotation.Pitch(), CarBody.WorldRotation.Yaw(), 0 );
		Rotation targetRotation = Rotation.From(angles);

		CarBody.SmoothRotate( targetRotation, 0.5f, 1/100 );
	}
}
