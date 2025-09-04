using ActInterfaces;
using UnityEngine;
using GeneralSets;

public interface IPlayerAttack : IAttackable, ICastable
{ }
public class PlayerAttackController : MonoBehaviour, IPlayerAttack
{
    [SerializeField] private MeleeAttack meleeAttack;
    [SerializeField] private PlayerStats stats;
    public float BaseCooldown => 0f; //쿨타임이 0이 되었을 경우에만 공격 가능한 기능이 필요함
    public float MaxCooldown => 0f; //이게 쿨타임 총량
    public float Cooldown => 0f; //이게 남은 쿨타임
    public void Attack(float attacker)
    {
        if (meleeAttack != null)
        {
            meleeAttack.Attack(attacker);
        }
    }
    public void Cast(CastKey key, float mana, Rigidbody2D target, bool absoluteTarget = false)
    {
        Debug.Log("Casted targeted spell");
        stats.ReduceStat(ReduceType.Mana, mana); //마나 소모됨
    }
    public void Cast(CastKey key, float mana, Vector2 area, bool penetration = false)
    {
        Debug.Log("Casted area spell");
        stats.ReduceStat(ReduceType.Mana, mana);
    }
}