using UnityEngine;
using System.Collections;
using CharacterSOInterfaces;

public class MeleeAttackRunner : MonoBehaviour, ISkillRunner
{
    Transform owner;
    MeleeAttackSpec spec;
    IHitboxSpec hitboxSpec;

    public Transform weaponPivot; // ������ �ڱ� transform ���
    Camera cam;
    float cdRemain;
    bool busy;

    public bool IsBusy => busy;
    public bool IsOnCooldown => cdRemain > 0f;

    public void Init(Transform owner, MeleeAttackSpec spec, IHitboxSpec hitboxSpec)
    {
        this.owner = owner;
        this.spec = spec;
        this.hitboxSpec = hitboxSpec;
        cam = Camera.main;

        weaponPivot = this.owner; // �ʿ� �� owner.Find("WeaponPivot")�� ��ü
    }

    void Update()
    {
        if (cdRemain > 0f) cdRemain -= Time.deltaTime;
        AimPivotToMouse();
    }

    void AimPivotToMouse()
    {
        if (!cam || !weaponPivot) return;
        var m = cam.ScreenToWorldPoint(Input.mousePosition);
        m.z = weaponPivot.position.z;
        var dir = (m - weaponPivot.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void TryCast()
    {
        if (busy || IsOnCooldown) return;
        StartCoroutine(CoCast());
    }

    IEnumerator CoCast()
    {
        busy = true;

        // 1) Windup (���� Attacker�� ����-���ε� ���� ���)  :contentReference[oaicite:10]{index=10}
        float baseAngle = weaponPivot.eulerAngles.z;
        float half = spec.swingAngle * 0.5f;
        float startAngle = baseAngle - half;
        float endAngle = baseAngle + half;

        float t = 0f;
        while (t < spec.windup)
        {
            t += Time.deltaTime;
            float a = Mathf.LerpAngle(baseAngle, startAngle, t / spec.windup);
            weaponPivot.rotation = Quaternion.Euler(0, 0, a);
            yield return null;
        }

        // 2) Active: ��Ÿ�� ��Ʈ�ڽ� ����(���赵 ���)  :contentReference[oaicite:11]{index=11}
        var go = new GameObject("RuntimeHitbox");
        go.transform.SetParent(weaponPivot, false);
        var rt = go.AddComponent<RuntimeAttackHitbox>();
        rt.Initialize(hitboxSpec, owner);

        // Active ���� ����
        t = 0f;
        while (t < spec.active)
        {
            t += Time.deltaTime;
            float a = Mathf.LerpAngle(startAngle, endAngle, t / spec.active);
            weaponPivot.rotation = Quaternion.Euler(0, 0, a);
            yield return null;
        }

        // 3) Recovery
        yield return new WaitForSeconds(spec.recover);

        cdRemain = spec.cooldown;
        busy = false;
    }
}