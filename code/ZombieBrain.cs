using Sandbox;
using static HealthSystem;

public enum ZombieState { Idle, Approach, Leap }

public sealed class ZombieBrain : Component, HealthSystem.IHealthEvent
{
	[Property] NavMeshAgent Agent;
	[Property] GameObject Player;
	[Property] Rigidbody Body;
	[Property] TextRenderer StateDebugText;
	[Property] float FollowCooldown = 1f;

	float DistanceToPlayer;

	ZombieState CurrentState;

	TimeUntil NextFollow;

	protected override void OnStart()
	{
		CurrentState = ZombieState.Idle;
	}

	void HealthSystem.IHealthEvent.OnDeath() 
	{
		GameObject.Destroy();
	}


	protected override void OnFixedUpdate()
	{
		DistanceToPlayer = (Player.WorldPosition - WorldPosition).Length;

		switch ( CurrentState ) 
		{ 
			case ZombieState.Idle:
				StateDebugText.Text = "Idle";

				Agent.Stop();
				if ( DistanceToPlayer < 5000 ) { CurrentState = ZombieState.Approach; }
				break;

			case ZombieState.Approach:
				StateDebugText.Text = "Approach";

				// Walk to Player
				Agent.MaxSpeed = 240;

				if ( NextFollow )
				{
					Agent.MoveTo( Player.WorldPosition );
					NextFollow = FollowCooldown;
				}
				if ( Body.WorldRotation.Forward.Angle( Player.WorldPosition - GameObject.WorldPosition ) > 1 )
				{
					Body.SmoothRotate( Rotation.LookAt( Player.WorldPosition - GameObject.WorldPosition, Vector3.Up ), 0.5f, 0.01f );
				}

				if ( DistanceToPlayer > 5000 ) { CurrentState = ZombieState.Idle; }
				if ( DistanceToPlayer < 1000 ) { CurrentState = ZombieState.Leap; }
				break;

			case ZombieState.Leap:
				StateDebugText.Text = "Leap";

				if ( !NextFollow ) { break; }
				// Leap onto Player
				Agent.MaxSpeed = 5000;
				Agent.Acceleration = 5000;
				Agent.MoveTo( Player.WorldPosition );
				if ( DistanceToPlayer > 1000 ) { CurrentState = ZombieState.Approach; }
				NextFollow = FollowCooldown;
				break;
		}



	}
}
