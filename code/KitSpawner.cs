using Sandbox;

public sealed class KitSpawner : Component
{
	[Property] GameObject KitPrefab;
	[Property] float SpawnCooldown;

	GameObject currentKit;

	TimeUntil NextSpawn;

	protected override void OnStart()
	{
		SpawnKit();
	}

	protected override void OnFixedUpdate()
	{
		// Solange currentKit Valid ist bleibt der NextSpawn auf Cooldown
		if (currentKit.IsValid() ) NextSpawn = SpawnCooldown;
		// GhostKit?

		// Wenn NextSpawn abgelaufen ist und es kein gültiges Kit gibt, spawne ein neues
		if ( NextSpawn && !currentKit.IsValid ) 
		{
			SpawnKit();
		}
	}

	void SpawnKit()
	{
		// Particle Effekt, Sound?
		currentKit = KitPrefab.Clone( WorldPosition + Vector3.Up * 500 );
	}
}
