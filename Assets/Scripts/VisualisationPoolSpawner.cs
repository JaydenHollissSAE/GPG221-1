using System.Collections;
using UnityEngine;

public class VisualisationPoolSpawner : MonoBehaviour
{
    public int spawnAmount = 100;
    public float spawnDelay = 5f;
    public GameObject prefab;

    public bool spawnerEnabled = true;
    public GameObject pool;

    void Start()
    {
        StartCoroutine(Spawn(spawnAmount));
    }

    public void DoSpawn(int amount = 1)
    {
        StartCoroutine(Spawn(amount));
    }

    IEnumerator Spawn(int amount)
    {
        int i = 0;
        if (spawnerEnabled)
        {
            while (i < amount)
            {
                yield return null;

                if (!spawnerEnabled) break;

                i++;
                GameObject spawned = Instantiate(prefab);
                spawned.transform.parent = pool.transform;
                VisualisationSetter.instance.visualisationPool.Push(spawned); 
                yield return new WaitForSeconds(spawnDelay);
            }
            yield return null;
        }
        yield return null;

    }
}
