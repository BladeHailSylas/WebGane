using CharacterSOInterfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeAttackHitbox : MonoBehaviour
{
    public IHitboxSpec spec;
    public Transform attacker; // 넉백 방향 계산 등에 사용(선택)

    private readonly HashSet<Collider2D> _hitOnce = new();
    private Collider2D _col;

    public void Initialize(IHitboxSpec s, Transform owner)
    {
        spec = s; attacker = owner;

        gameObject.layer = LayerMask.NameToLayer(spec.HitboxLayerName);

        // 콜라이더 동적 생성
        switch (spec.Shape)
        {
            case HitboxShape2D.Box:
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = spec.Size;
                _col = box;
                break;
            case HitboxShape2D.Capsule:
                var cap = gameObject.AddComponent<CapsuleCollider2D>();
                cap.isTrigger = true;
                cap.size = spec.Size;
                cap.direction = spec.CapsuleDirection;
                _col = cap;
                break;
        }

        // 로컬 오프셋(무기 피벗 전방)
        transform.SetLocalPositionAndRotation((Vector3)spec.LocalOffset, Quaternion.identity);
        //transform.localPosition = (Vector3)spec.LocalOffset;
        //transform.localRotation = Quaternion.identity;

        // 수명 타이머 시작
        StartCoroutine(Life());
    }

    IEnumerator Life()
    {
        float t = spec.ActiveTime;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject); // 풀 사용 시엔 반환으로 교체
    }

    void OnEnable() => _hitOnce.Clear();

    void OnTriggerEnter2D(Collider2D other)
    {
        // 대상 레이어 필터
        if (((1 << other.gameObject.layer) & spec.EnemyMask) == 0) return;
        if (_hitOnce.Contains(other)) return;
        _hitOnce.Add(other);

        // 공격 처리 — 사용자 게임의 인터페이스에 맞춰 호출
        // 업로드 코드에서 IVulnerable.TakeDamage(damage, apratio) 사용 중(:contentReference[oaicite:2]{index=2})
        if (other.TryGetComponent(out ActInterfaces.IVulnerable vuln))
        {
            vuln.TakeDamage(spec.Damage, spec.ApRatio);
        }

        // 넉백이 필요하다면, 상대가 Rigidbody2D를 갖고 있을 때만 처리
        if (attacker != null && other.attachedRigidbody != null)
        {
            Vector2 dir = (other.bounds.center - attacker.position).normalized;
            other.attachedRigidbody.AddForce(dir * spec.Knockback, ForceMode2D.Impulse);
        }
    }
}