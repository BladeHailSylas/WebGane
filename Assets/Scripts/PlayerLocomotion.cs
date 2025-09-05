using ActInterfaces;
using StatsInterfaces;
using UnityEngine;
using System.Collections;
public interface IPlayerLocomotion : IMovable, IVulnerable, IKnockbackable
{ }
public class PlayerLocomotion : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStats stats;
    private Vector2 movement;
    public void Move(Vector2 move, Rigidbody2D rb)
    {
        //while(!stats.HasEffect(StatsInterface.Effects.Stun))
        {
            rb.linearVelocity = move.normalized * stats.GetStat(StatType.Velocity, StatRef.Base);
        }
    }
    public void ApplyKnockback(Vector2 direction, float force, float time, bool isFixed)
    {
        Debug.Log("Knockback applied");
    }
    public void Jump(float duration, float wait = 1f)
    {
        StartCoroutine(CoJump(duration, wait));
    }
    public IEnumerator CoJump(float duration, float wait)
    {
        stats.StatSwitch(StatBool.OnGround);
        yield return new WaitForSeconds(duration);
        stats.StatSwitch(StatBool.OnGround);
        yield return new WaitForSeconds(wait);
    }
}