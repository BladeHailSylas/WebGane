// Enemy에 붙입니다. 아주 단순한 체력/넉백 처리 예시입니다.
using UnityEngine;
using ActInterfaces;
using System;
using System.IO;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDummy : MonoBehaviour, IVulnerable
{
    public float BasicHealth { get; private set; } = 10f;
    public float MaxHealth { get; private set; } = 15f;
    public float Health { get; private set; }

    public float BasicArmor { get; private set; } = 40f;
    public float Armor { get; private set; }

    public bool IsDead { get; private set; }
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        Health = MaxHealth;
        float ArmorIncreaseRate = 0.2f; // 방어력 20% 증가
        Armor = BasicArmor * (1 + ArmorIncreaseRate);
        Debug.Log($"Enemy info: Health {Health}, Armor {Armor}");
    }

    public void TakeDamage(float damage, float apratio, bool isFixed)
    {
        if (isFixed)
        {
            Health -= damage;
        }
        else
        {
            Health -= damage * (80 / (80 + Armor * (1 - apratio)));
        }
        Debug.Log($"Enemy took {damage * (80 / (80 + Armor * (1 - apratio)))} damage");
        if (Health <= 0f) Die();
    }
    System.Collections.IEnumerator Flash()
    {
        var original = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        sr.color = original;
    }
    public void Die()
    {
        // TODO: 사망 연출
        Debug.Log("Now that's a LOTTA damage");
        IsDead = true;
        Destroy(gameObject);
    }
}
