using Sandbox;
using System;

public sealed class ZombieSpawner : Component
{
	[Property] GameObject ZombiePrefab;
	[Property] float SpawnCooldown;
	[Property] float ScaleFactor;

	GameObject currentZombie;

	TimeUntil NextSpawn;
	Random random;

	protected override void OnStart()
	{
		random = new Random();

		SpawnZombie();
	}

	protected override void OnFixedUpdate()
	{
		// Solange currentKit Valid ist bleibt der NextSpawn auf Cooldown
		if ( currentZombie.IsValid() ) NextSpawn = SpawnCooldown;
		// GhostKit?

		// Wenn NextSpawn abgelaufen ist und es kein gültiges Kit gibt, spawne ein neues
		if ( NextSpawn && !currentZombie.IsValid )
		{
			SpawnZombie();
		}
	}

	void SpawnZombie()
	{
		// Particle Effekt, Sound?
		currentZombie = ZombiePrefab.Clone( WorldPosition, WorldRotation, Vector3.One * ScaleFactor * random.Float(0.9f, 1.1f) );
	}
}
