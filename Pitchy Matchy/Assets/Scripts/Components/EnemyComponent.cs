using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyComponent : MonoBehaviour
{
    [SerializeField] SpriteRenderer enemySprite;
    [SerializeField] Color damagedColor;
    [SerializeField] float damagedFlashDuration;
    [SerializeField] float enemyHP;
    [SerializeField] int attackPower;
    [SerializeField] Slider hpBar;
    private bool isDefeated;
    private Color defaultColor;
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
        defaultColor = enemySprite.color;
        maxhp = enemyHP;
    }
    public void TakeDamage(int damage)
    {
        Debug.Log($"Enemy takes {damage} damage!");

        StartCoroutine(HurtFlash());

        if ((enemyHP - damage) < 0)
        {
            isDefeated = true;
            enemyHP = 0;
            return;
        }

        enemyHP -= damage;
    }

    private IEnumerator HurtFlash()
    {
        enemySprite.color = damagedColor;
        yield return new WaitForSeconds(damagedFlashDuration);
        enemySprite.color = defaultColor;
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
