using SOInterfaces;
using UnityEngine;
using ActInterfaces;

[System.Serializable]
public class MeleeParams : ISkillParam, IHasCooldown
{
    public float radius = 1.6f, angleDeg = 120f;
    public float windup = 0.05f, recover = 0.08f, cooldown = 0.10f;
    public float damage = 10f, knockback = 6f, apRatio = 0f;
    public LayerMask enemyMask;
    public float Cooldown => cooldown;
}

[CreateAssetMenu(menuName = "Mechanics/MeleeInstant")]
public class MeleeInstantMechanic : SkillMechanicBase<MeleeParams>

{
    public override System.Collections.IEnumerator Cast(Transform owner, Camera cam, MeleeParams meleeParams)
    {
        if (meleeParams.windup > 0f) yield return new WaitForSeconds(meleeParams.windup);

        Vector2 origin = owner.position;
        Vector2 fwd = GetMouseDir(cam, origin);
        float half = meleeParams.angleDeg * 0.5f;

        var raw = Physics2D.OverlapCircleAll(origin, meleeParams.radius, meleeParams.enemyMask);
        foreach (var collider in raw)
        {
            if (meleeParams.angleDeg < 359f)
            {
                Vector2 to = (Vector2)collider.bounds.ClosestPoint(origin) - origin;
                if (to.sqrMagnitude > 1e-6f) //if 분기점이 이래도 되는가? if not으로 접근해서 if를 줄인다면? -> 그러기엔 else가 없다. 이대로만 가죠?
                {
                    float angle = Vector2.SignedAngle(fwd, to.normalized);
                    if (Mathf.Abs(angle) > half) continue;
                }
            }
            if (collider.TryGetComponent(out IVulnerable vul))
                vul.TakeDamage(meleeParams.damage, meleeParams.apRatio);

            if (collider.attachedRigidbody)
            {
                var dir = ((Vector2)collider.bounds.center - origin).normalized;
                collider.attachedRigidbody.AddForce(dir * meleeParams.knockback, ForceMode2D.Impulse);
            }
        }

        if (meleeParams.recover > 0f) yield return new WaitForSeconds(meleeParams.recover);
    }

    static Vector2 GetMouseDir(Camera cam, Vector2 from)
    {
        if (!cam) return Vector2.right;
        var m = cam.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0f; return ((Vector2)m - from).normalized; //m.z한데요
    }
}