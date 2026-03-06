using Sandbox;

public sealed class ZombieBrain : Component
{
	[Property] NavMeshAgent Agent;
	[Property] GameObject Player;
	[Property] Rigidbody Body;
	[Property] float FollowCooldown = 1f;
	TimeUntil NextFollow;

	protected override void OnFixedUpdate()
	{

		if ( NextFollow )
		{
			Agent.MoveTo( Player.WorldPosition );
			NextFollow = FollowCooldown;
		}
		if ( Body.WorldRotation.Forward.Angle( Player.WorldPosition - GameObject.WorldPosition ) > 1 ) 
		{
			Body.SmoothRotate( Rotation.LookAt( Player.WorldPosition - GameObject.WorldPosition, Vector3.Up ), 0.5f, 0.01f);
		}


	}
}
