using ActInterfaces;
using UnityEngine;
using GeneralSets;

public interface IPlayerAttack : IAttackable, ICastable
{ }
public class PlayerAttackController : MonoBehaviour, IPlayerAttack
{
    [SerializeField] private MeleeAttack meleeAttack;
    [SerializeField] private PlayerStats stats;
    public float BaseCooldown => 0f; //��Ÿ���� 0�� �Ǿ��� ��쿡�� ���� ������ ����� �ʿ���
    public float MaxCooldown => 0f; //�̰� ��Ÿ�� �ѷ�
    public float Cooldown => 0f; //�̰� ���� ��Ÿ��
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