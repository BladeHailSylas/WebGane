// MeleeAttack.cs
// Player(혹은 Player 하위 적당한 오브젝트)에 붙입니다.
// WeaponPivot과 Hitbox를 Inspector에서 연결하세요.
using UnityEngine;
using System.Collections;
using ActInterfaces;
using Unity.VisualScripting;

public class MeleeAttack : MonoBehaviour, IAttackable
{
    [Header("Refs")]
    public Transform weaponPivot;     // 회전 중심
    public MeleeHitbox hitbox;        // isTrigger 콜라이더 보유 오브젝트
    public SpriteRenderer debugBlade; // (선택) 막대 시각화용

    [Header("Timing (seconds)")]
    public float windup = 0.06f;      // 예비 동작
    public float active = 0.08f;      // 히트박스 활성·스윙
    public float recover = 0.10f;     // 후딜
    public float cooldown = 0.08f;    // 연속 입력 제한

    [Header("Swing")]
    [Tooltip("스윙 총 각도(육십분법)")]
    public float swingAngle = 120f;
    public float pivotToBlade; //= 0.8f; // 피벗→칼날 거리(디버그 막대 길이용)

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

        // 마우스 방향으로 피벗을 향하게(시각적 조준)
        AimPivotToMouse();

        // 디버그 블레이드(막대) 길이/방향 갱신(선택)
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

        // 1) Windup: 피벗을 기준으로 반대편으로 약간 틀어 “예비자세”
        float half = swingAngle * 0.5f;
        float startAngle = weaponPivot.eulerAngles.z - half;
        float endAngle = weaponPivot.eulerAngles.z + half;

        // Windup 동안 살짝 뒤로 빼는 맛(선택)
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

        // 2) Active: 히트박스 On + start→end로 스윕
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

        // 3) Recovery: 후딜(자유 회전 금지)
        yield return new WaitForSeconds(recover);

        // 4) Cooldown
        _coolRemain = cooldown;

        _busy = false;

        _secondAttack ^= true; // 다음 공격은 반대로 공격
    }
}
