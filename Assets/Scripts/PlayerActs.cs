using ActInterfaces;
using StatsInterfaces;
using System.Collections;
using UnityEngine;
using GeneralSets;
public class PlayerActs : MonoBehaviour, IAffectable, IPlayerLocomotion //스탯 참조 및 관리 필요, MeleeHitbox의 주석 참조
{
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerLocomotion locomotion;
    public void TakeDamage(float damage, float apratio, bool isFixed)
    {
        stats.ReduceStat(ReduceType.Health, damage, apratio, isFixed);
    }
    public void Die()
    {
        //죽는 효과(죽메) 등 여기에 추가 가능
        Destroy(gameObject);
    }
    public void ApplyKnockback(Vector2 direction, float force, float time, bool isFixed)
    {
        locomotion.ApplyKnockback(direction, force, time, isFixed);
    }
    public void Move(Vector2 move, Rigidbody2D rb)
    {
        locomotion.Move(move, rb);
    }
    public void ApplyEffect(Effects buffType, float duration, float Amplifier = 0)
    {
        stats.AddEffect(buffType, duration, Amplifier);
    }
    public void Cleanse(Effects buffType)
    {
        if (buffType == Effects.None)
        {
            stats.ClearNegative();
        }
        else
        {
            stats.RemoveEffect(buffType);
        }
    }
    public void Jump(float duration, float wait = 1f)
    {
        locomotion.Jump(duration, wait);
    }
}