using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComponent : MonoBehaviour
{
    [SerializeField] int hp; //equivalent to no. of tries
    [SerializeField] Sprite playerSprite;
    [SerializeField] int attackPower;
    private bool playerDefeated;

    public void TakeDamage(int damage)
    {
        Debug.Log($"Player takes {damage} damage!");
        if ((hp - damage) < 0)
        {
            playerDefeated = true;
            hp = 0;
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

    void Start()
    {
        playerDefeated = false;
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
}
