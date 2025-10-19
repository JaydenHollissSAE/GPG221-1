using System.Collections;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    public int spawnAmount = 10;
    public float spawnDelay = 5f;
    public GameObject prefab;

    public bool spawnerEnabled = true;

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
                yield return new WaitForSeconds(spawnDelay);

                if (!spawnerEnabled) break;

                i++;
                GameObject spawned = Instantiate(prefab);
                spawned.transform.position = AIGrid.instance.walkableGrid[Random.Range(0, AIGrid.instance.walkableGrid.Count)].position; // Spawns in a random walkable cell
                spawned.transform.parent = transform;
            }
        }

    }
}
