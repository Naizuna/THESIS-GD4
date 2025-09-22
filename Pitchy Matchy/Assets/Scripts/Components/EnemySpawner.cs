using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] QuizController quizHandler;
    [SerializeField] GameObject spawnPoint;
    [SerializeField] GameObject[] enemyVariants;
    [SerializeField] bool isInfiniteSpawning;
    [SerializeField] int numberOfSpawns;
    [SerializeField] bool autoSpawn;
    [SerializeField] float zSpawnOffset;
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
        if (autoSpawn)
        {
            if (spawnedEnemy == null)
                TrySpawn();
        }
    }

    void TrySpawn()
    {
        if (variantCount == 0 || spawnPoint == null) return;

        if (numberOfSpawns == 0) return; // spawn tokens

        SpawnEnemy();

        if (numberOfSpawns > 0) numberOfSpawns--;
    }

    public void SpawnEnemy()
    {
        int i = Random.Range(0, variantCount);
        GameObject temp = Instantiate(
            enemyVariants[i], 
            new Vector3(spawnPoint.transform.position.x, spawnPoint.transform.position.y, zSpawnOffset), 
            Quaternion.identity
        );
        spawnedEnemy = temp;
        quizHandler.SetEnemy(spawnedEnemy.GetComponent<EnemyComponent>());
    }
}
