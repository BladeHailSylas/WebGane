// Enemy에 붙입니다. 아주 단순한 체력/넉백 처리 예시입니다.
using UnityEngine;
using ActInterfaces;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDummy : MonoBehaviour, IVulnerable //그냥 임시 더미, 절대 이렇게 만들면 안 됨
{
    public float BasicHealth { get; private set; } = 100f;
    public float MaxHealth { get; private set; } = 150f;
    public float Health { get; private set; }

    public float BasicArmor { get; private set; } = 40f;
    public float Armor { get; private set; }

    public bool IsDead { get; private set; }

    private float _armorIncreaseRate = 0f; //방어력 버프
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        Health = MaxHealth;
        Armor = BasicArmor * (1 + _armorIncreaseRate);
        Debug.Log($"Enemy info: Health {Health}, Armor {Armor}");
    }
    void Update()
    {
        float _tempArmor = BasicArmor * (1 + _armorIncreaseRate);
        if (Armor != _tempArmor)
        {
            Armor = _tempArmor;
            Debug.Log($"Enemy info: Health {Health}, Armor {Armor}");
        }
    }

    public void TakeDamage(float damage, float apratio, bool isFixed)
    {
        if (isFixed)
        {
            Health -= damage;
        }
        else
        {
            Health -= damage * (80 / (80 + Armor * (1 - apratio))); // 대미지 * 피해율, 피해율 산출을 하나의 메서드로 사용? << PlayerStats의 ReduceStat에 구현됨
            _armorIncreaseRate += 0.1f; // 피격 시마다 방어력 10% 증가(임시 버프, 실제 버프는 이렇게 적용하지 않음)
            StartCoroutine(Flash());
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
