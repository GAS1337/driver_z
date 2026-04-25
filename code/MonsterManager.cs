using Sandbox;
using System;
using System.Linq;

public sealed class MonsterManager : Component
{
    [Property] GameObject MonsterSpawnerPrefab;
    [Property] GameObject ZombiePrefab;
    [Property] GameObject GhostPrefab;
    [Property] GameObject VampirePrefab;

	[Property] BBox SpawnArea;

	[Property] Curve ZombieCurve;
    [Property] Curve GhostCurve;
    [Property] Curve VampireCurve;
    float TotalSpawnerLimit;

    List<GameObject> ActiveZombieSpawner;
    List<GameObject> ActiveGhostSpawner;
    List<GameObject> ActiveVampireSpawner;

    [Property] float CloneFrequenz;

    TimeSince SinceGameStart;
    TimeUntil UntilNextClone;
    Random random;

    protected override void OnStart()
    {
        ActiveZombieSpawner = new List<GameObject>();
        ActiveGhostSpawner = new List<GameObject>();
        ActiveVampireSpawner = new List<GameObject>();

        SinceGameStart = 0;
        UntilNextClone = CloneFrequenz;

        random = new Random();
    }

    protected override void OnFixedUpdate()
    {
		DebugOverlay.Box( SpawnArea, Color.Green );
		// Listen aufräumen bevor neue Spawner gecloned werden

		ActiveZombieSpawner.RemoveAll( x => !x.IsValid );
        ActiveGhostSpawner.RemoveAll( x => !x.IsValid );
        ActiveVampireSpawner.RemoveAll( x => !x.IsValid );


        int GesamtzahlAktiveSpawner = ActiveZombieSpawner.Count 
            + ActiveGhostSpawner.Count 
            + ActiveVampireSpawner.Count;
        
        TotalSpawnerLimit = ZombieCurve.Evaluate(SinceGameStart / 60) 
            + GhostCurve.Evaluate(SinceGameStart / 60) 
            + VampireCurve.Evaluate(SinceGameStart / 60);

		if (GesamtzahlAktiveSpawner >= TotalSpawnerLimit) { UntilNextClone = CloneFrequenz; }

        // Wenn das Limit nicht auschgeschöpft ist und UntilNextClone, dann im Verhältnis niedrigsten vorhandenen Spawner clonen
        if ((float)GesamtzahlAktiveSpawner < TotalSpawnerLimit && UntilNextClone)
        {
            float zombieDifferenz = ZombieCurve.Evaluate(SinceGameStart / 60) - ActiveZombieSpawner.Count;
            float ghostDifferenz = GhostCurve.Evaluate(SinceGameStart / 60) - ActiveGhostSpawner.Count;
            float vampireDifferenz = VampireCurve.Evaluate(SinceGameStart / 60) - ActiveVampireSpawner.Count;

            zombieDifferenz = zombieDifferenz.Remap(0, ZombieCurve.Evaluate(SinceGameStart / 60));
            ghostDifferenz = ghostDifferenz.Remap(0, GhostCurve.Evaluate(SinceGameStart / 60));
            vampireDifferenz = vampireDifferenz.Remap(0, VampireCurve.Evaluate(SinceGameStart/ 60));

            float[] differenzen = {zombieDifferenz, ghostDifferenz, vampireDifferenz};

            // Index von größter Differenz finden und entsprechendes Prefab clonen
            int differenzenMaxIndex = Array.IndexOf(differenzen, differenzen.Max());
            switch (differenzenMaxIndex)
            {
                default: 
                    Log.Error("MonsterManager: OnFixedUpdate() DEFAULT");
                    break;

                case 0: // Zombie
                    CloneSpawner(ZombiePrefab, differenzenMaxIndex);
                    break;

                case 1: // Ghost
                    CloneSpawner(GhostPrefab, differenzenMaxIndex);
                    break;

                case 2: // Vampire
                    CloneSpawner(VampirePrefab, differenzenMaxIndex);
                    break;
            }
            UntilNextClone = CloneFrequenz;
        }

    }

    void CloneSpawner(GameObject prefab, int indexCase)
    {
		// Scale? Spawnfreq? Spawnanzahl?

		// Valid Spawnpos check
		Vector3 possibleSpawnPosition = SpawnArea.RandomPointInside;

		SceneTraceResult spawnPosTrace = Scene.Trace.Sphere(300, possibleSpawnPosition, possibleSpawnPosition + Vector3.Down * 1000)
			.WithoutTags("world", "enemy")
			.Run();

		while ( spawnPosTrace.Hit ) 
		{
			possibleSpawnPosition = SpawnArea.RandomPointInside;

			spawnPosTrace = Scene.Trace.Sphere( 300, possibleSpawnPosition, possibleSpawnPosition + Vector3.Down * 1000 )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "World", "enemy" )
				.Run();
			
			DebugOverlay.Trace(spawnPosTrace);
			Log.Info( "Spawnpos trace hit, trying new position");
		}

		spawnPosTrace = Scene.Trace.Ray( possibleSpawnPosition, possibleSpawnPosition + Vector3.Down * 1000 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithTag( "World" )
			.Run();
		Vector3 actualSpawnPosition = possibleSpawnPosition.WithZ(spawnPosTrace.EndPosition.z);

		GameObject newSpawner = MonsterSpawnerPrefab.Clone(actualSpawnPosition, WorldRotation.Angles().WithYaw(random.Int(0, 359)), Vector3.One);
        newSpawner.GetComponent<MonsterSpawner>().MonsterPrefab = prefab;


		Log.Info( "Cloning spawner for prefab: " + prefab.Name + " at " + newSpawner.WorldPosition );

		switch (indexCase)
        {
            default:
                Log.Error("Monster Manager: CloneSpawner() DEFAULT");
                break;

            case 0: // Zombie
                ActiveZombieSpawner.Add(newSpawner);
                break;

            case 1: // Ghost
                ActiveGhostSpawner.Add(newSpawner);
                break;

            case 2: // Vampire
                ActiveVampireSpawner.Add(newSpawner);
                break;
        }
    }
}
