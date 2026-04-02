using Sandbox;
using System.Numerics;

public sealed class HealthSystem : Component
{
	[Property] float SetHealth;
	[Property] SpriteRenderer HealthbarRenderer;

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
		if ( HealthbarRenderer != null ) 
		{
			HealthbarRenderer.Size += new Vector2(-amount.Remap( 0, SetHealth, 0, 200 ), 0);
			Log.Info( -amount.Remap( 0, SetHealth, 0, 200 ) );
			HealthbarRenderer.Color = HealthbarRenderer.Color.AdjustHue( -amount.Remap( 0, SetHealth, 0, 120 ) );
		} 

		if ( CurrentHealth <= 0 ) 
		{
			HealthbarRenderer.Color = HealthbarRenderer.Color.WithAlpha( 0 );
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
