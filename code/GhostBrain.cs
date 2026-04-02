using Sandbox;
using System;
using static Ballistics;
using static HealthSystem;

public enum GhostState { Wander, IsMoving, Approach, Attack, Staggered }


public sealed class GhostBrain : Component, HealthSystem.IHealthEvent
{
	[Property] NavMeshAgent Agent;
	[Property] GameObject Player;
	[Property] TextRenderer StateDebugText;

	public GhostState CurrentState;

	Random Random;


	void IHealthEvent.OnDeath()
	{
		GameObject.Parent.Destroy();
	}

	protected override void OnStart()
	{
		Random = new Random();

		CurrentState = GhostState.Wander;
	}

	protected override void OnUpdate()
	{
		switch ( CurrentState ) 
		{
			default: 
				StateDebugText.Text = "DEFAULT";
				Agent.Stop(); 
				break;

			// WANDER
			case GhostState.Wander: 
				StateDebugText.Text = "WANDER";
				// Choose a random position to move to within a certain radius, reset move speed to default
				Agent.MaxSpeed = 120;
				Agent.MoveTo( Agent.WorldPosition + new Vector3 (Random.Int(-500, 500), Random.Int(-500, 500), 0) );
				CurrentState = GhostState.IsMoving;

				break;

			case GhostState.IsMoving:
				StateDebugText.Text = "IS MOVING";

				// Choose new Pos if we are close to the current target
				if ( Agent.TargetPosition.HasValue ) 
				{
					if ( ((Vector3)Agent.TargetPosition - Agent.WorldPosition).IsNearlyZero( 10 ) ) { CurrentState = GhostState.Wander; }
				}

				// Check for Distance to Player and switch to Approach if close enough
				if ( (Agent.WorldPosition - Player.WorldPosition).Length < 7000 ) 
				{ 
					CurrentState = GhostState.Approach;
				}

				break;
			// APPROACH
			case GhostState.Approach:
				// Go to wander if distance too far
				if ( (Agent.WorldPosition - Player.WorldPosition).Length > 7000 )
				{
					CurrentState = GhostState.Wander;
				}
				Agent.MaxSpeed = 360;
				Agent.MoveTo( Player.WorldPosition + Player.GetComponent<Rigidbody>().Velocity.Normal * 1000 );

				break;

			// ATTACK
			case GhostState.Attack:


				break;

			// STAGGERED
			case GhostState.Staggered:


				break;
		}

	}


}
