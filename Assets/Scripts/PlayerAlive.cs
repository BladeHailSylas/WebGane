using UnityEngine;
using System.Collections;
using Generals;
public class PlayerAlive : MonoBehaviour//, IPlayerCharacter -> �׳� SO�� �����ϴ� ����, SO �������� �����̶� ���� namespace�� �־ �� ��
{
    [SerializeField] PlayerStats stats;
    public void StatsInitialize()
    {
        
    }
}

/*PlayerAlive�� ������ �ɻ�ġ �ʴ�
 * PlayerAlive�� �׳� ĳ���͸� �ν��Ͻ�ȭ�ϴ� ��ü�� �־� �ϳ�?
 * �ƴϸ� PlayerAlive ��ü�� �������ͽ��� �ൿ �޼��带 �������� �־� �ϳ�?
 * ������ ��ħ�� �����ڸ� ���� �� ����
 * ���� PlayerAttackController�� ������ ������ �� ��ų�� Empty�� �־�ΰ� ���� ��� ��
 * �׷��� �̰͵��� �� MonoBehaviour�� �ֵ� �Ǵ� ���ΰ�? MonoBehaviour�� ��Ȯ�� ����?
 * ��Ÿ�� ����� �����԰� ���� ����� ������ ����ϸ� Empty�� �־�δ� �� ����� ����
 * PlayerAttackController�͵� ������ �ߵǴ� ���� �Ƚ�
 * �׷��� �׷��� PlayerAlive�� �ʹ� ���� SerializedField�� ����, �ʹ� ���� ��ü�� ������ �����ؾ� �Ѵ�
 * Stats�� ��쿡�� BaseStat�� �����ϸ� ������ �� ���� ������ ����
*/