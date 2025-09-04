// MeleeHitbox.cs
// Hitbox 오브젝트(Trigger Collider 보유)에 붙입니다.
// 스윙 시작 시 Enable, 끝나면 Disable. 활성 동안 충돌한 Enemy에 1회씩만 데미지 전송.
using UnityEngine;
using System.Collections.Generic;
using ActInterfaces;

[RequireComponent(typeof(Collider2D))]
public class MeleeHitbox : MonoBehaviour
{
    public float damage = 12f;
    public float apratio = 0f;
    public float knockback = 7f;
    public LayerMask enemyMask; // Enemy 레이어만 맞도록

    // 공격자(주로 Player) Transform — 넉백 방향 계산에 필요
    [HideInInspector] public Transform attacker;

    private readonly HashSet<Collider2D> _hitThisSwing = new(); // readonly이긴 한데 이거 뭔지 모르겠음. 추측컨대 피격된 Collider 집합체인 것 같은데 readonly에 문제가 없는지 확인해야 할 듯

    void OnEnable()
    {
        _hitThisSwing.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(damage);
        if (((1 << other.gameObject.layer) & enemyMask) == 0) return;
        if (_hitThisSwing.Contains(other)) return;

        _hitThisSwing.Add(other); //readonly인데 Add는 가능함. 아마 인스턴스만 readonly이고 메서드 참조는 가능한 듯

        //Vector2 dir = (other.bounds.center - attacker.position);
        if (other.TryGetComponent<IVulnerable>(out var dmg))
        {
            dmg.TakeDamage(damage, apratio);
        }
    }
}
