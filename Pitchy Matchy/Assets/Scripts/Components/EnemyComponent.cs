using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyComponent : MonoBehaviour
{
    [SerializeField] Sprite enemySprite;
    [SerializeField] int enemyHP;
    [SerializeField] int attackPower;
    private bool isDefeated;
    
    public void TakeDamage(int damage)
    {
        Debug.Log($"Enemy takes {damage} damage!");
        if ((enemyHP - damage) < 0)
        {
            isDefeated = true;
            enemyHP = 0;
            return;
        }

        enemyHP -= damage;
    }

    public bool IsDefeated()
    {
        return isDefeated;
    }

    public int GetAttackPower()
    {
        return attackPower;
    }


    void Start()
    {
        isDefeated = false;
    }
    void Update()
    {
        ///testing purposes
        ///
        if (isDefeated)
        {
            Debug.Log("Enemy Defeated");
        }
    }
}
