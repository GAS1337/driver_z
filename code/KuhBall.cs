using Sandbox;
using System;
using System.Threading.Tasks;

public sealed class KuhBall : Component
{
	[Property] SoundEvent IdleSound;
	[Property] public SoundEvent DamageSound;
	Rigidbody Body;
	Rotation targetRot;
	float TurnCooldown = 4f;
	TimeUntil NextTurn;
	Random random;

	protected override void OnStart()
	{
		random = new Random();
		Body = GameObject.GetComponent<Rigidbody>();
	}

	protected override void OnUpdate()
	{
		SceneTraceResult GroundCheck = Scene.Trace.Ray(WorldPosition, WorldPosition + WorldRotation.Down * 1)
			.IgnoreGameObjectHierarchy(GameObject)
			.WithoutTags("enemy", "player")
			.Run();
		// DebugOverlay.Trace(GroundCheck);

		if ( GroundCheck.Hit ) 
		{
			if ( NextTurn ) 
			{
				targetRot = WorldRotation.Angles().WithYaw(WorldRotation.Yaw() + random.Int(-45, 45)).WithPitch(0);
				NextTurn = TurnCooldown;
				if ( random.Int( 1, 10 ) > 5 )
				{
					SoundHandle mooHandle = Sound.Play( IdleSound, WorldPosition );
					mooHandle.Parent = GameObject;
					mooHandle.FollowParent = true;
				}
			}

			Body.SmoothRotate(targetRot, 0.6f, Time.Delta);

		}
		else if (Body.Velocity.Length < 10)
		{
			// Log.Info("Trying rotate");
			Body.SmoothRotate( targetRot.Angles().WithYaw(WorldRotation.Yaw()), 0.4f, Time.Delta );

		}
		
		
	}

}
