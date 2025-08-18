using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComponent : MonoBehaviour
{
    [SerializeField] int hp; //equivalent to no. of tries
    [SerializeField] SpriteRenderer playerSprite;
    [SerializeField] Color damagedColor;
    [SerializeField] int attackPower;
    [SerializeField] float damagedFlashDuration;
    private bool playerDefeated;
    private Color defaultColor;

    public void TakeDamage(int damage)
    {
        Debug.Log($"Player takes {damage} damage!");
        StartCoroutine(HurtFlash());

        if ((hp - damage) <= 0)
        {
            playerDefeated = true;
            hp = 0;
            Death();
            return;
        }

        hp -= damage;
    }

    public bool IsPlayerDefeated()
    {
        return playerDefeated;
    }

    public int GetAttackPower()
    {
        return attackPower;
    }

    public void Death()
    {
        Destroy(this.gameObject);
    }

    void Start()
    {
        playerDefeated = false;
        defaultColor = playerSprite.color;
    }

    void Update()
    {
        ///testing purposes
        ///
        if (playerDefeated)
        {
            Debug.Log("Player Defeated");
        }
    }

    private IEnumerator HurtFlash()
    {
        playerSprite.color = damagedColor;
        yield return new WaitForSeconds(damagedFlashDuration);
        playerSprite.color = defaultColor;
    }
}
