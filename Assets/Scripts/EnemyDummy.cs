// Enemy�� ���Դϴ�. ���� �ܼ��� ü��/�˹� ó�� �����Դϴ�.
using UnityEngine;
using ActInterfaces;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDummy : MonoBehaviour, IVulnerable //�׳� �ӽ� ����, ���� �̷��� ����� �� ��
{
    public float BasicHealth { get; private set; } = 100f;
    public float MaxHealth { get; private set; } = 150f;
    public float Health { get; private set; }

    public float BasicArmor { get; private set; } = 40f;
    public float Armor { get; private set; }

    public bool IsDead { get; private set; }

    private float _armorIncreaseRate = 0f; //���� ����
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
            Health -= damage * (80 / (80 + Armor * (1 - apratio))); // ����� * ������, ������ ������ �ϳ��� �޼���� ���? << PlayerStats�� ReduceStat�� ������
            _armorIncreaseRate += 0.1f; // �ǰ� �ø��� ���� 10% ����(�ӽ� ����, ���� ������ �̷��� �������� ����)
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
        // TODO: ��� ����
        Debug.Log("Now that's a LOTTA damage");
        IsDead = true;
        Destroy(gameObject);
    }
}
