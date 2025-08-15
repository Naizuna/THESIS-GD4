using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] GameObject spawnPoint;
    [SerializeField] GameObject[] enemyVariants;
    private GameObject spawnedEnemy;
    private int variantCount;

    void Start()
    {
        variantCount = enemyVariants.Length;
    }

    void Update()
    {
        if (spawnedEnemy is null)
            SpawnEnemy();
    }

    void SpawnEnemy()
    {
        int i = Random.Range(0, variantCount);
        GameObject temp = Instantiate(
            enemyVariants[i], spawnPoint.transform.position, Quaternion.identity);
        spawnedEnemy = temp;
    }
}
