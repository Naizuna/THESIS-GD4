using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EnemyComponent : MonoBehaviour
{
    [SerializeField] SpriteRenderer enemySprite;
    [SerializeField] Animator animator;
    [SerializeField] ParticleSystem hurtEffect;
    [SerializeField] ParticleSystem deathEffect;
    [SerializeField] Color damagedColor;
    [SerializeField] float damagedFlashDuration;
    [SerializeField] public int maxhp;
    public int currHP { get; private set; }
    [SerializeField] int attackPower;
    [SerializeField] float deathDuration;
    public bool isDefeated  {  get; private set; }
    private Color defaultColor;
    private GameObject enemySpriteParentObj;
    
    void Update()
    {
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
        enemySpriteParentObj = enemySprite.gameObject;
        currHP = maxhp;
    }
    public void TakeDamage(int damage)
    {
        Debug.Log($"Enemy takes {damage} damage!");

        StartCoroutine(HurtFlash());

        if ((currHP - damage) <= 0)
        {
            isDefeated = true;
            currHP = 0;
            StartCoroutine(Death());
            return;
        }

        currHP -= damage;
    }

    public void PlayAttack()
    {
        animator.SetBool("isAttacking", true);
    }

    public void StopAttack()
    {
        animator.SetBool("isAttacking", false);
    }

    public void StopDeathAnim()
    {
        animator.SetBool("isDeath", false);
    }

    private IEnumerator HurtFlash()
    {
        PlayHitEffect();
        animator.SetBool("isHurt", true);
        yield return new WaitForSeconds(damagedFlashDuration);
        animator.SetBool("isHurt", false);
    }

    public IEnumerator Death()
    {
        PlayDeathEffect();
        animator.SetBool("isDeath", true);
        yield return new WaitForSeconds(deathDuration);
        Destroy(gameObject);
    }

    void PlayHitEffect() 
    {
        ParticleSystem instance = Instantiate(
            hurtEffect, enemySpriteParentObj.transform.position,
            Quaternion.identity
        );
        Destroy(instance.gameObject,
        instance.main.duration + 
        instance.main.startLifetime.constantMax);
    }

    void PlayDeathEffect() 
    {
        ParticleSystem instance = Instantiate(
            deathEffect, enemySpriteParentObj.transform.position,
            Quaternion.identity
        );
        Destroy(instance.gameObject,
        instance.main.duration + 
        instance.main.startLifetime.constantMax);
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
