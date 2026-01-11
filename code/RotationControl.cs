using Sandbox;

public sealed class RotationControl : Component
{
	[Property] Rigidbody CarBody;

	protected override void OnFixedUpdate()
	{
		Rotation oldRotation = CarBody.WorldRotation;
		Angles angles = new Angles( 0 , oldRotation.Yaw(), 0 );
		Rotation targetRotation = Rotation.From(angles);

		CarBody.SmoothRotate( targetRotation, 0.5f, Time.Delta );
	}
}
