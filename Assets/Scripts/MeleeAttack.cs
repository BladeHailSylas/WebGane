// MeleeAttack.cs
// Player(Ȥ�� Player ���� ������ ������Ʈ)�� ���Դϴ�.
// WeaponPivot�� Hitbox�� Inspector���� �����ϼ���.
using UnityEngine;
using System.Collections;
using ActInterfaces;
using Unity.VisualScripting;

public class MeleeAttack : MonoBehaviour, IAttackable
{
    [Header("Refs")]
    public Transform weaponPivot;     // ȸ�� �߽�
    public MeleeHitbox hitbox;        // isTrigger �ݶ��̴� ���� ������Ʈ
    public SpriteRenderer debugBlade; // (����) ���� �ð�ȭ��

    [Header("Timing (seconds)")]
    public float windup = 0.06f;      // ���� ����
    public float active = 0.08f;      // ��Ʈ�ڽ� Ȱ��������
    public float recover = 0.10f;     // �ĵ�
    public float cooldown = 0.08f;    // ���� �Է� ����

    [Header("Swing")]
    [Tooltip("���� �� ����(���ʺй�)")]
    public float swingAngle = 120f;
    public float pivotToBlade; //= 0.8f; // �ǹ���Į�� �Ÿ�(����� ���� ���̿�)

    [Header("Debug")]
    public float MaxCooldown { get; private set; }
    public float BasicCooldown { get; private set; }
    public float Cooldown { get; private set; }


    private Camera _cam;
    private bool _busy;
    private float _coolRemain;
    private bool _secondAttack;

    void Awake()
    {
        _cam = Camera.main;
        if (hitbox) hitbox.attacker = transform;
        if (hitbox) hitbox.gameObject.SetActive(false);
        if (debugBlade) debugBlade.enabled = true;
        BasicCooldown = cooldown;
        Cooldown = BasicCooldown;
    }

    void Update()
    {
        _coolRemain = Mathf.Max(0f, _coolRemain - Time.deltaTime);

        // ���콺 �������� �ǹ��� ���ϰ�(�ð��� ����)
        AimPivotToMouse();

        // ����� ���̵�(����) ����/���� ����(����)
        if (debugBlade)
        {
            debugBlade.transform.localPosition = new Vector3(pivotToBlade * 0.5f, 0, 0);
            debugBlade.size = new Vector2(debugBlade.size.x, pivotToBlade);
        }

        if (Input.GetMouseButtonDown(0))
            Attack();
    }

    void AimPivotToMouse()
    {
        if (!_cam || !weaponPivot) return;
        Vector3 mouse = _cam.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = weaponPivot.position.z;
        Vector2 dir = (mouse - weaponPivot.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void Attack()
    {
        if (_busy || _coolRemain > 0f) return;
        StartCoroutine(CoAttack());
    }

    public IEnumerator CoAttack()
    {
        _busy = true;

        // 1) Windup: �ǹ��� �������� �ݴ������� �ణ Ʋ�� �������ڼ���
        float half = swingAngle * 0.5f;
        float startAngle = weaponPivot.eulerAngles.z - half;
        float endAngle = weaponPivot.eulerAngles.z + half;

        // Windup ���� ��¦ �ڷ� ���� ��(����)
        float t = 0f;
        float windStart = weaponPivot.eulerAngles.z;
        float windTarget = startAngle;
        while (t < windup)
        {
            t += Time.deltaTime;
            float k = t / windup;
            float a = Mathf.LerpAngle(windStart, windTarget, k);
            weaponPivot.rotation = Quaternion.Euler(0, 0, a);
            yield return null;
        }

        // 2) Active: ��Ʈ�ڽ� On + start��end�� ����
        if (hitbox) hitbox.gameObject.SetActive(true);

        t = 0f;
        while (t < active)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / active);
            float a = Mathf.LerpAngle(startAngle, endAngle, k);
            weaponPivot.rotation = Quaternion.Euler(0, 0, a);
            yield return null;
        }

        if (hitbox) hitbox.gameObject.SetActive(false);

        // 3) Recovery: �ĵ�(���� ȸ�� ����)
        yield return new WaitForSeconds(recover);

        // 4) Cooldown
        _coolRemain = cooldown;

        _busy = false;

        _secondAttack ^= true; // ���� ������ �ݴ�� ����
    }
}
