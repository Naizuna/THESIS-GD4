using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] QuizHandler quizHandler;
    [SerializeField] GameObject spawnPoint;
    [SerializeField] GameObject[] enemyVariants;
    [SerializeField] bool isInfiniteSpawning;
    [SerializeField] int numberOfSpawns;
    private GameObject spawnedEnemy;
    private int variantCount;

    void Start()
    {
        variantCount = enemyVariants.Length;

        if (isInfiniteSpawning)
            numberOfSpawns = -1;
    }

    void Update()
    {
        if (spawnedEnemy == null)
            TrySpawn();
    }

    void TrySpawn()
    {
        if (variantCount == 0 || spawnPoint == null) return;

        if (numberOfSpawns == 0) return; // spawn tokens

        SpawnEnemy();

        if (numberOfSpawns > 0) numberOfSpawns--;
    }

    void SpawnEnemy()
    {
        int i = Random.Range(0, variantCount);
        GameObject temp = Instantiate(
            enemyVariants[i], spawnPoint.transform.position, Quaternion.identity);
        spawnedEnemy = temp;
        quizHandler.SetEnemy(spawnedEnemy.GetComponent<EnemyComponent>());
    }
}
