using Sandbox;
using System;
using static HealthSystem;
using static Sandbox.ModelPhysics;

public sealed class MonsterSpawner : Component, HealthSystem.IHealthEvent
{
	[Property] GameObject DeadMonsterSpawner;
	[Property] public LineRenderer LineRenderer; 
	[Property] public GameObject MonsterPrefab; // Wird von MonsterManager gepassed
	public float MonsterSpawnStartDelay = 1f;
	public float MonsterSpawnCooldown = 1f;
	public float MonsterScaleFactor = 1f;
	[Property] public int MaxMonsterSpawns = 1;

	List<GameObject> SpawnedMonsters;

	TimeUntil UntilNextSpawn;
	Random random;

	public interface IMonsterSpawnerEvent : ISceneEvent<IMonsterSpawnerEvent>
	{
		void OnMonsterSpawn();
	}

	void IHealthEvent.OnDeath()
	{
		SpawnedMonsters.RemoveAll( x => !x.IsValid );
		/* AUSGESCHALTET!!!! --------------
		foreach (var monster in SpawnedMonsters)
		{
			if ( monster == null ) { return; }
			// HealthSystem holen, wenn nicht da aus Children holen und damagen
			HealthSystem healthSystem = monster.GetComponent<HealthSystem>();
			if ( healthSystem != null ) healthSystem.Damage( 500 );
			else { monster.GetComponentInChildren<HealthSystem>().Damage( 500 ); }
		} */
		GameObject newDeadMonsterSpawner = DeadMonsterSpawner.Clone(WorldPosition + Vector3.Up, WorldRotation, WorldScale);
		foreach ( GameObject child in newDeadMonsterSpawner.Children ) 
		{
			child.GetComponent<Rigidbody>().ApplyImpulse( (child.WorldPosition - GameObject.WorldPosition).Normal * 3 * child.GetComponent<Rigidbody>().Mass );
			child.GetComponent<Rigidbody>().AngularVelocity = random.VectorInSphere( random.Float( 3, 5 ) );
		}
	}

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
		IMonsterSpawnerEvent.PostToGameObject( this.GameObject, x => x.OnMonsterSpawn() );
	}
}
