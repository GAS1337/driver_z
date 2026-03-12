using Sandbox;
using static HealthSystem;

public enum ZombieState { Idle, Approach, Leap, Staggered }

public sealed class ZombieBrain : Component, HealthSystem.IHealthEvent
{
	[Property] NavMeshAgent Agent;
	[Property] GameObject Player;
	[Property] Rigidbody Body;
	[Property] TextRenderer StateDebugText;
	[Property] float FollowCooldown = 1f;

	float DistanceToPlayer;

	public ZombieState CurrentState;

	TimeUntil NextFollow;
	TimeUntil KnockBack;

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
			default:
				Agent.Stop();
				break;
			case ZombieState.Idle:
				StateDebugText.Text = "Idle";

				Agent.Stop();
				if ( DistanceToPlayer < 5000 ) { CurrentState = ZombieState.Approach; }
				KnockBack = 1f;
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
				KnockBack = 1f;
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
				KnockBack = 1f;
				break;

			case ZombieState.Staggered:
				Agent.Stop();
				Agent.UpdatePosition = false;
				Agent.SetAgentPosition( Agent.WorldPosition );

				SceneTraceResult groundCheck = Scene.Trace.Ray( Body.WorldPosition + Body.WorldRotation.Up * 10, Body.WorldPosition + Body.WorldRotation.Down * 20 )
					.Radius( 1 )
					.IgnoreGameObjectHierarchy( GameObject )
					.Run();

				if (KnockBack && groundCheck.Hit) 
				{

					Agent.UpdatePosition = true;
					CurrentState = ZombieState.Idle;
				}

				break;
		}



	}
}
