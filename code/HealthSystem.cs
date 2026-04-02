using Sandbox;
using System.Numerics;

public sealed class HealthSystem : Component, HealthSystem.IHealthEvent
{
	[Property] float SetHealth;
	[Property] SpriteRenderer HealthbarRenderer;

	HighscoreManager HighscoreManager;

	public float CurrentHealth;

	public interface IHealthEvent : ISceneEvent<IHealthEvent>
	{
		void OnDeath();
	}

	void IHealthEvent.OnDeath() 
	{ 
	}

	protected override void OnStart()
	{
		HighscoreManager = Scene.Get<HighscoreManager>();
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
			if (HealthbarRenderer != null) HealthbarRenderer.Color = HealthbarRenderer.Color.WithAlpha( 0 );
			Log.Info( "Killed " + GameObject.Name );
			if (GameObject.Tags.Has("enemy")) HighscoreManager.IncreaseScore(SetHealth);
			if (GameObject.Tags.Has("player")) HighscoreManager.ResetScore();
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
