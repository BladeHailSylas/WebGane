using StatsInterfaces;
using ActInterfaces;
using UnityEngine;
using EffectInterfaces;
public class PlayerActController : MonoBehaviour, IVulnerable, IKnockbackable //IAffectable //이게 Controller의 역할을 수행하게 될 듯
{
    [SerializeField] PlayerStats stats;
    [SerializeField] PlayerLocomotion locomotion;
    [SerializeField] PlayerEffects effects;
    //Movable
    private Rigidbody2D rig;

    void Awake()
    {
        rig = GetComponent<Rigidbody2D>();
    }
    public void MakeMove(Vector2 move)
    {
        if(move != null && effects.IsMovable) 
        {
            locomotion.Move(move, rig,stats.GetStat(StatType.Velocity, StatRef.Current));
        }
    }

    //Vulnerable
    public void TakeDamage(float damage, float apratio, bool isFixed)
    {
        stats.ReduceStat(ReduceType.Health, damage, apratio, isFixed);
    }
    public void Die()
    {
        //죽는 효과(죽메) 등 여기에 추가 가능
        Destroy(gameObject);
    }

    //Knockbackable
    public void ApplyKnockback(Vector2 direction, float force)
    {
        locomotion.ApplyKnockback(direction, force);
    }
}