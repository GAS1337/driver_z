using Sandbox;
using System;

public sealed class RammingControl : Component, Component.ITriggerListener
{
	[Property] Rigidbody CarBody;

	public void OnTriggerEnter( GameObject other )
	{
		if ( !other.Tags.Has( "enemy" ) ) return;
		if (CarBody.Velocity.Length < 800) return;
		Log.Info( $"Rammed {other.Name}" );

		// Stagger and Knock Zombie
		if ( other.GetComponent<ZombieBrain>() != null )
		{
			other.GetComponent<ZombieBrain>().CurrentState = ZombieState.Staggered;
			other.GetComponent<ZombieBrain>().KnockBack = 
				Math.Max( CarBody.Velocity.Length.Remap( 0, 4000), other.GetComponent<ZombieBrain>().KnockBack + CarBody.Velocity.Length.Remap( 0, 4000) );
		}

		// Apply Impulse and Damage to Enemy
		if ( other.GetComponent<Rigidbody>() != null ) other.GetComponent<Rigidbody>().ApplyImpulse(Vector3.Up * CarBody.Velocity.Length.Remap( 0, 4000, 0, 100000 ) );
		
		// Damage
		other.GetComponent<HealthSystem>().Damage( CarBody.Velocity.Length.Remap(0, 4000, 0, 250) );
		// Damage MonsterSpawner
		if ( other.GetComponent<MonsterSpawner>() != null ) other.GetComponent<HealthSystem>().Damage( CarBody.Velocity.Length.Remap(0, 4000, 0, 5000) );

		Sound.Play( "sounds/bullet-impact-flesh.sound", WorldPosition);
		if ( other.Tags.Has( "cow" ) ) 
		{ 
			SoundHandle screamHandle = Sound.Play( other.GetComponent<KuhBall>().DamageSound, other.WorldPosition );
			screamHandle.Parent = other;
			screamHandle.FollowParent = true;
		}
	}
}
