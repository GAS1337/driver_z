using Sandbox;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

public sealed class HealthSystem : Component, HealthSystem.IHealthEvent
{
	[Property] public float SetHealth;
	[Property] SpriteRenderer HealthbarRenderer;
	IEnumerable<ModelRenderer> ModelRendererList;
	Color originalTint;

	List<GameObject> LootList;

	HighscoreManager HighscoreManager;

	public float CurrentHealth;

	TimeSince TimeSinceLastDamage;
	Random Random;

	public interface IHealthEvent : ISceneEvent<IHealthEvent>
	{
		void OnDeath();
	}

	void IHealthEvent.OnDeath() 
	{
		if ( GameObject.Tags.Has( "enemy" ) )
		{
			if ( Random.Int( 1, 10 ) > 7 )
			{
				Log.Info( "Dropping loot" );
				LootList[Random.Int(0,2)].Clone( WorldPosition + Vector3.Up * 200 );
			}
			Log.Info( "Enemy " + GameObject.Name + " died." );
			GameObject.Destroy();
		}
	}

	protected override void OnStart()
	{
		HighscoreManager = Scene.Get<HighscoreManager>();
		ModelRendererList = GetComponentsInChildren<ModelRenderer>();
		originalTint = ModelRendererList.First<ModelRenderer>().Tint;

		CurrentHealth = SetHealth;

		// LootList wird nur für Gegner erstellt, da Spieler keine Lootdrops haben
		if ( !GameObject.Tags.Has( "enemy" ) ) return;

		Random = new Random();
		LootList = new List<GameObject>();
		LootList.Add( GameObject.GetPrefab( "prefabs/medikit.prefab" ) );
		LootList.Add( GameObject.GetPrefab( "prefabs/ammokit.prefab" ) );
		LootList.Add( GameObject.GetPrefab( "prefabs/pointkit.prefab" ) );

	}

	public void Damage( float amount, bool enableSound = true )
	{
		if ( CurrentHealth <= 0 ) return;

		CurrentHealth = (CurrentHealth - amount).Clamp( 0, SetHealth );
		ApplyDamageTint();

		if ( HealthbarRenderer != null ) 
		{
			HealthbarRenderer.Size += new Vector2(-amount.Remap( 0, SetHealth, 0, 200 ), 0);
			// Log.Info( -amount.Remap( 0, SetHealth, 0, 200 ) );
			HealthbarRenderer.Color = HealthbarRenderer.Color.AdjustHue( -amount.Remap( 0, SetHealth, 0, 120 ) );
		} 

		if ( CurrentHealth <= 0 ) 
		{
			if (HealthbarRenderer != null) HealthbarRenderer.Color = HealthbarRenderer.Color.WithAlpha( 0 );
			// Log.Info( "Killed " + GameObject.Name );
			if (GameObject.Tags.Has("enemy")) HighscoreManager.IncreaseScore(SetHealth);
			IHealthEvent.PostToGameObject( this.GameObject, x => x.OnDeath() );
		}
		if ( GameObject.Tags.Has( "player" ) && enableSound == true )
		{
			Sound.Play( "sounds/metal-hit-cartoon.sound", Scene.FindAllWithTag("carbody").First<GameObject>().WorldPosition );
		}
	}

	[Button]
	async Task ApplyDamageTint() 
	{
		foreach ( var renderer in ModelRendererList )
		{
			renderer.Tint = Color.Average(new Color[] { Color.Red, Color.White } ) ;
		}

		TimeSinceLastDamage = 0;
		await Task.DelaySeconds( 0.2f );
		
		if (!this.IsValid) return;
		if (TimeSinceLastDamage < 0.2f) return;

		foreach ( var renderer in ModelRendererList )
		{
			renderer.Tint = originalTint;
		}
	}

	[Button]
	void Damage500()
	{
		Damage(500);
	}

	[Button]
	void Heal500()
	{
		Damage( -500 );
	}


}
