using ActInterfaces;
using UnityEngine;
using Generals;

public interface IPlayerAttack : IAttackable, ICastable
{ }
public class PlayerAttackController : MonoBehaviour
{
    [SerializeField] private MeleeAttack meleeAttack; // 이거 이렇게 둬야 하는지 확인해야 됨
    [SerializeField] private PlayerStats stats;
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