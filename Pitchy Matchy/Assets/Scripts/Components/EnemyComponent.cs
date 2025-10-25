using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EnemyComponent : MonoBehaviour
{
    SpriteRenderer enemySprite;
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
        enemySprite = GetComponent<SpriteRenderer>();
        isDefeated = false;
        defaultColor = enemySprite.color;
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

    private IEnumerator HurtFlash()
    {
        enemySprite.color = damagedColor;
        PlayHitEffect();
        yield return new WaitForSeconds(damagedFlashDuration);
        enemySprite.color = defaultColor;
    }

    public IEnumerator Death()
    {
        enemySprite.enabled = false;
        PlayDeathEffect();
        yield return new WaitForSeconds(deathDuration);
        Destroy(gameObject);
    }

    void PlayHitEffect() 
    {
        ParticleSystem instance = Instantiate(
            hurtEffect, transform.position,
            Quaternion.identity
        );
        Destroy(instance.gameObject,
        instance.main.duration + 
        instance.main.startLifetime.constantMax);
    }

    void PlayDeathEffect() 
    {
        ParticleSystem instance = Instantiate(
            deathEffect, transform.position,
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
