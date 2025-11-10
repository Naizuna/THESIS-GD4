using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerComponent : MonoBehaviour
{
    [SerializeField] public int hp; //equivalent to no. of tries
    [SerializeField] public int currHp { get; private set; }
    [SerializeField] SpriteRenderer playerSprite;
    [SerializeField] Color immunityColor;
    [SerializeField] int attackPower;
    [SerializeField] float immunityFlashDuration;
    [SerializeField] int flashTimes;
    public bool isImmune { get; set; }
    private bool playerDefeated;
    private Color defaultColor;
    private Animator animator;

    public void TakeDamage(int damage)
    {
        HurtFlash();
        if (isImmune)
        {
            Debug.Log("Player is immune to damage");
            return;
        }

        if ((currHp - damage) <= 0)
        {
            playerDefeated = true;
            currHp = 0;
            PlayDeath();
            return;
        }

        Debug.Log($"Player takes {damage} damage!");
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

    public void PlayDeath()
    {
        animator.SetBool("isDeath", true);
    }

    public void PlayAttack()
    {
        animator.SetBool("isAttacking", true);
    }

    public void StopAttack()
    {
        animator.SetBool("isAttacking", false);
    }

    public void OnDeathAnimationFinish()
    {
        animator.SetBool("isDeath", false);
        Destroy(gameObject);
    }

    void Start()
    {
        animator = GetComponent<Animator>();
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

    public void HurtFlash()
    {
        if (isImmune)
        {
            ImmunityFlash();
            return;
        }
        animator.SetBool("isHurt", true);
    }

    public void StopHurtFlash()
    {
        animator.SetBool("isHurt", false);
    }

    public void ImmunityFlash()
    {
        StartCoroutine(ImmunityFlashCoroutine());
    }

    public IEnumerator ImmunityFlashCoroutine()
    {
        for (int i = 0; i < flashTimes; i++)
        {
            playerSprite.color = immunityColor;
            yield return new WaitForSeconds(immunityFlashDuration);
            playerSprite.color = defaultColor;
        }
    }
}
