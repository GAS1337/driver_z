using Sandbox;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

public sealed class MonsterManager : Component
{
    [Property] GameObject MonsterSpawnerPrefab;
    [Property] GameObject ZombiePrefab;
    [Property] GameObject GhostPrefab;
    [Property] GameObject VampirePrefab;

    Curve ZombieCurve;
    Curve GhostCurve;
    Curve VampireCurve;
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
        // Listen aufräumen bevor neue Spawner gecloned werden
        if (UntilNextClone)
        {
            ActiveZombieSpawner.RemoveAll( x => !x.IsValid );
            ActiveGhostSpawner.RemoveAll( x => !x.IsValid );
            ActiveVampireSpawner.RemoveAll( x => !x.IsValid );
        }

        int GesamtzahlAktiveSpawner = ActiveZombieSpawner.Count 
            + ActiveGhostSpawner.Count 
            + ActiveVampireSpawner.Count;
        
        TotalSpawnerLimit = ZombieCurve.Evaluate(SinceGameStart / 60) 
            + GhostCurve.Evaluate(SinceGameStart / 60) 
            + VampireCurve.Evaluate(SinceGameStart / 60);
        
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



        GameObject newSpawner = MonsterSpawnerPrefab.Clone();
        newSpawner.GetComponent<MonsterSpawner>().MonsterPrefab = prefab;

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
