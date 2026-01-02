using Sandbox;
using static Sandbox.ModelPhysics;

public sealed class CameraController : Component
{
	[Property] CameraComponent main_camera;
	[Property] Rigidbody body;
	[Property] int DistanceToPlayer = 2000;

	Vector3 currPos;
	Vector3 oldPos;
	Vector3 ToPlayer;

	protected override void OnUpdate()
	{
		Angles mouseMove = Input.AnalogLook;
		currPos = body.WorldPosition;

		if ( Input.MouseWheel.y < 0 ) { DistanceToPlayer += 50; }
		if ( Input.MouseWheel.y > 0 ) { DistanceToPlayer -= 50; }

		main_camera.WorldPosition += currPos - oldPos;
		ToPlayer = body.WorldPosition - main_camera.WorldPosition;
		main_camera.WorldPosition += (main_camera.WorldRotation.Right * (mouseMove.yaw * 10)) + (main_camera.WorldRotation.Up * (mouseMove.pitch * 10));
		if ( !ToPlayer.Length.AlmostEqual(DistanceToPlayer, 1f) ) 
		{
			main_camera.WorldPosition = body.WorldPosition - ToPlayer.Normal * DistanceToPlayer;
		}
		oldPos = currPos;

		main_camera.WorldRotation = Rotation.LookAt( body.WorldPosition - main_camera.WorldPosition, Vector3.Up );

		if ( main_camera.WorldRotation.Angles().pitch > 90 && main_camera.WorldRotation.Angles().pitch < -90 )
		{
			Log.Info( main_camera.WorldRotation.Angles().pitch );
		}
	}
}
