using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComponent : MonoBehaviour
{
    [SerializeField] public int hp; //equivalent to no. of tries
    [SerializeField] public int currHp { get; private set; }
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

        if ((currHp - damage) <= 0)
        {
            playerDefeated = true;
            currHp = 0;
            Death();
            return;
        }

        currHp -= damage;
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
        this.gameObject.SetActive(false);
    }

    void Start()
    {
        playerDefeated = false;
        defaultColor = playerSprite.color;
        currHp = hp;
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
