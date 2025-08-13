using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyComponent : MonoBehaviour
{
    [SerializeField] Sprite enemySprite;
    [SerializeField] float enemyHP;
    [SerializeField] int attackPower;
    [SerializeField] Slider hpBar;
    private bool isDefeated;
    float maxhp;
    void Update()
    {
        hpBar.value = Mathf.Clamp01(enemyHP / maxhp);
        ///testing purposes
        ///
        if (isDefeated)
        {
            Debug.Log("Enemy Defeated");
        }
    }

    private void Start()
    {
        isDefeated = false;
        maxhp = enemyHP;
    }
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
}
