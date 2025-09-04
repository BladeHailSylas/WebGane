using ActInterfaces;
using UnityEngine;
using Generals;

public interface IPlayerAttack : IAttackable, ICastable
{ }
public class PlayerAttackController : MonoBehaviour
{
    [SerializeField] private MeleeAttack meleeAttack; // �̰� �̷��� �־� �ϴ��� Ȯ���ؾ� ��
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
        stats.ReduceStat(ReduceType.Mana, mana); //���� �Ҹ��
    }
    public void Cast(CastKey key, float mana, Vector2 area, bool penetration = false)
    {
        Debug.Log("Casted area spell");
        stats.ReduceStat(ReduceType.Mana, mana);
    }
}