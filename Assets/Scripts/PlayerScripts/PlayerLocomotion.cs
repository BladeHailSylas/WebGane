using ActInterfaces;
using StatsInterfaces;
using UnityEngine;
using System.Collections;
public interface IPlayerLocomotion : IMovable, IVulnerable, IKnockbackable
{ }
public class PlayerLocomotion : MonoBehaviour, IMovable, IKnockbackable
{
    [SerializeField] private Rigidbody2D rb;
    public Vector2 LastMoveVector { get; private set; }
    //[SerializeField] private PlayerStats stats;
    //private Vector2 movement;
    public void Move(Vector2 move, Rigidbody2D rb, float velocity)
    {
        LastMoveVector = move; //(0,0)이어도 상관 없음 -> 이동하지 않는 상태니까
        rb.linearVelocity = move.normalized * velocity;
    }
    public void ApplyKnockback(Vector2 direction, float force)
    {
        rb.linearVelocity += direction * force;
    }
    public void Jump(float duration, float wait = 1f)
    {
        StartCoroutine(CoJump(duration, wait));
    }
    public IEnumerator CoJump(float duration, float wait)
    {
        //stats.StatSwitch(StatBool.OnGround); 이벤트 발생으로 대체하라
        Debug.Log("Yahoo");
        yield return new WaitForSeconds(duration);
        //stats.StatSwitch(StatBool.OnGround);
        yield return new WaitForSeconds(wait);
    }
}