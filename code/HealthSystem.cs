using Sandbox;
using System.Numerics;

public sealed class HealthSystem : Component
{
	[Property] float SetHealth;

	public float CurrentHealth;

	public interface IHealthEvent : ISceneEvent<IHealthEvent>
	{
		void OnDeath();
	}

	protected override void OnStart()
	{
		CurrentHealth = SetHealth;
	}

	public void Damage( float amount ) 
	{
		CurrentHealth = CurrentHealth - amount;
		if ( CurrentHealth <= 0 ) 
		{ 
			Log.Info( "Killed "+GameObject.Name );
			IHealthEvent.PostToGameObject( this.GameObject, x => x.OnDeath() );
		}
	}

	[Button]
	void Damage500( float amount )
	{
		Damage(500);
	}

	[Button]
	void Heal500( float amount )
	{
		Damage( -500 );
	}


}
