using Sandbox;
using System;

public sealed class MonsterSpawner : Component
{
	[Property] public GameObject MonsterPrefab; // Wird von MonsterManager gepassed
	public float MonsterSpawnStartDelay = 1f;
	public float MonsterSpawnCooldown = 1f;
	public float MonsterScaleFactor = 1f;
	public int MaxMonsterSpawns = 1;

	List<GameObject> SpawnedMonsters;


	TimeUntil UntilNextSpawn;
	Random random;

	protected override void OnStart()
	{
		SpawnedMonsters = new List<GameObject>();

		random = new Random();

		UntilNextSpawn = MonsterSpawnStartDelay;
	}

	protected override void OnFixedUpdate()
	{
		// Solange Max Monsters gespawned sind ist bleibt der NextSpawn auf Cooldown
		if ( SpawnedMonsters.Count >= MaxMonsterSpawns ) UntilNextSpawn = MonsterSpawnCooldown;

		if (true)
		{
		    SpawnedMonsters.RemoveAll( x => !x.IsValid );	
		}

        // Wenn NextSpawn abgelaufen ist
		if ( UntilNextSpawn )
		{
			SpawnMonster();
			UntilNextSpawn = MonsterSpawnCooldown;
		}
	}

	void SpawnMonster()
	{
		// Particle Effekt, Sound?
		GameObject currentMonster = MonsterPrefab.Clone( WorldPosition + (Vector3)random.VectorInCircle() * random.Int(200, 500), WorldRotation, Vector3.One * MonsterScaleFactor * random.Float(0.9f, 1.1f) );
		SpawnedMonsters.Add(currentMonster);
	}
}
