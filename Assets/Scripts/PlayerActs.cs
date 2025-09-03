using ActInterfaces;
using StatsInterfaces;
using System.Collections;
using UnityEngine;
public interface IPlayerActs : IMovable, IVulnerable, IAffectable, IKnockbackable
{
    //플레이어가 할 수 있는 행동들에 대한 인터페이스
}
public class PlayerActs : MonoBehaviour, IPlayerActs //스탯 참조 및 관리 필요, MeleeHitbox의 주석 참조
{
    [SerializeField] private PlayerStats stats;
    public void TakeDamage(float damage, float apratio, bool isFixed)
    {
        stats.ReduceStat(true, damage, apratio, isFixed);
        if (stats.Health <= 0f) Die();
    }
    public void Die()
    {
        if (stats.IsDead) Destroy(gameObject);
    }
    public void ApplyKnockback(Vector2 direction, float force, float time, bool isFixed)
    {
        throw new System.NotImplementedException();
    }
    public void Move(Vector2 move, Rigidbody2D rb)
    {
        //while(!stats.HasEffect(StatsInterface.Effects.Stun))
        {
            rb.linearVelocity = move.normalized * stats.GetStat(StatType.Velocity, StatRef.Base);
        }
    }
    public void ApplyEffect(ActInterfaces.Effects buffType, float duration, float Amplifier = 0)
    {
        stats.AddEffect((StatsInterfaces.Effects)buffType, duration, Amplifier);
    }
    public void Cleanse(ActInterfaces.Effects buffType)
    {
        stats.RemoveEffect(buffType);
    }
    public void Jump(float duration)
    {
        StartCoroutine(CoJump(duration));

    }
    public IEnumerator CoJump(float duration)
    {
        stats.StatSwitch(StatBool.OnGround);
        yield return new WaitForSeconds(duration);
        stats.StatSwitch(StatBool.OnGround);
    }
}