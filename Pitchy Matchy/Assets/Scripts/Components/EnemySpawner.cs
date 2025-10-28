using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

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
    private EnemyComponent spawnedEnemyComponent;
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
            {
                TrySpawn();
                quizHandler.playerInputEnabled = true;
            }
            if (spawnedEnemyComponent.isDefeated)
            {
                quizHandler.playerInputEnabled = false;
            }
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
            spawnPoint.transform.position,
            Quaternion.identity
        );
        spawnedEnemy = temp;
        spawnedEnemyComponent = spawnedEnemy.GetComponentInChildren<EnemyComponent>();
        quizHandler.SetEnemy(spawnedEnemyComponent);
    }

    public void StopSpawn()
    {
        autoSpawn = false;
        numberOfSpawns = 0;
        Destroy(spawnedEnemy);
    }
}
