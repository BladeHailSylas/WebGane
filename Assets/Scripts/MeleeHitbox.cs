// MeleeHitbox.cs
// Hitbox 오브젝트(Trigger Collider 보유)에 붙입니다.
// 스윙 시작 시 Enable, 끝나면 Disable. 활성 동안 충돌한 Enemy에 1회씩만 데미지 전송.
using UnityEngine;
using System.Collections.Generic;
using ActInterfaces;
using System;

[RequireComponent(typeof(Collider2D))]
public class MeleeHitbox : MonoBehaviour
{
    public float damage = 12f;
    public float knockback = 7f;
    public LayerMask enemyMask; // Enemy 레이어만 맞도록

    // 공격자(주로 Player) Transform — 넉백 방향 계산에 필요
    [HideInInspector] public Transform attacker;

    private HashSet<Collider2D> _hitThisSwing = new();

    void OnEnable()
    {
        _hitThisSwing.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyMask) == 0) return;
        if (_hitThisSwing.Contains(other)) return;

        _hitThisSwing.Add(other);

        Vector2 dir = (other.bounds.center - attacker.position);
        if (other.TryGetComponent<IVulnerable>(out var dmg)) // 인터페이스 가져오는 이 기능으로 플레이어 스탯 참조 시도
        {
            dmg.TakeDamage(damage);
        }
    }
}
