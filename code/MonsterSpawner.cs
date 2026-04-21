using Sandbox;
using System;
using System.ComponentModel;

public sealed class MonsterSpawner : Component
{
	[Property] public GameObject MonsterPrefab; // Wird von MonsterManager gepassed
	public float MonsterSpawnDelay = 1f;
	public float MonsterSpawnCooldown = 1f;
	public float MonsterScaleFactor = 1f;
	public int MaxMonsterSpawns = 3;

	GameObject currentMonster;
	List<GameObject> SpawnedMonsters;


	TimeUntil UntilNextSpawn;
	Random random;

	protected override void OnStart()
	{
		SpawnedMonsters = new List<GameObject>();

		random = new Random();

		UntilNextSpawn = SpawnDelay;
	}

	protected override void OnFixedUpdate()
	{
		// Solange currentMonster Valid ist bleibt der NextSpawn auf Cooldown
		if ( currentMonster.IsValid() ) UntilNextSpawn = SpawnCooldown;

		if (UntilNextSpawn)
		{
		    SpawnedMonsters.RemoveAll( x => !x.IsValid );	
		}

        // Wenn NextSpawn abgelaufen ist und es kein gültiges Monster gibt, spawne ein neues
		if ( UntilNextSpawn && (!currentMonster.IsValid() || currentMonster == null) )
		{
			SpawnMonster();
		}
	}

	void SpawnMonster()
	{
		// Particle Effekt, Sound?
		currentMonster = MonsterPrefab.Clone( WorldPosition, WorldRotation, Vector3.One * ScaleFactor * random.Float(0.9f, 1.1f) );
	}
}
