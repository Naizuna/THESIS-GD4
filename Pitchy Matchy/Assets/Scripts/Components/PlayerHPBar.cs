using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class PlayerHPBar : MonoBehaviour
{
    [SerializeField] PlayerComponent player;
    [SerializeField] SpriteRenderer sr;
    [SerializeField] Sprite[] hpBarSprite;
    
    void Update()
    {
        UpdateHPBar();
    }
    
    void UpdateHPBar()
    {
        // Calculate the percentage of HP remaining (0.0 to 1.0)
        float hpPercentage = Mathf.Clamp01((float)player.currHp / player.hp);
        // Map the percentage to sprite index (0 to 9)
        // Multiply by 10 to get a value from 0-10, then round down
        int spriteIndex = Mathf.FloorToInt(hpPercentage * 10);
        
        // Clamp to valid array indices (0-9)
        spriteIndex = Mathf.Clamp(spriteIndex, 0, hpBarSprite.Length - 1);
        
        // Apply the sprite
        sr.sprite = hpBarSprite[spriteIndex];
    }
}