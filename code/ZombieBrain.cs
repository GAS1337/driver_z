using Sandbox;

public sealed class ZombieBrain : Component
{
	[Property] NavMeshAgent Agent;
	[Property] GameObject Player;
	[Property] Rigidbody Body;
	protected override void OnFixedUpdate()
	{
		Agent.MoveTo( Player.WorldPosition );
		if ( Body.WorldRotation.Forward.Angle( Player.WorldPosition - GameObject.WorldPosition ) > 1 ) 
		{
			Body.SmoothRotate( Rotation.LookAt( Player.WorldPosition - GameObject.WorldPosition, Vector3.Up ), 0.5f, 0.01f);
		}
	}
}
