using UnityEngine;
using System.Collections;
using Generals;
public class PlayerAlive : MonoBehaviour//, IPlayerCharacter -> 그냥 SO로 생성하는 역할, SO 가져오는 역할이랑 같은 namespace에 있어도 될 듯
{
    [SerializeField] PlayerStats stats;
    public void StatsInitialize()
    {
        
    }
}

/*PlayerAlive의 역할이 심상치 않다
 * PlayerAlive는 그냥 캐릭터를 인스턴스화하는 객체로 둬야 하나?
 * 아니면 PlayerAlive 자체가 스테이터스와 행동 메서드를 가지도록 둬야 하나?
 * 기존의 방침을 따르자면 위가 더 좋음
 * 현재 PlayerAttackController의 역할을 따르면 각 스킬을 Empty에 넣어두고 꺼내 써야 함
 * 그런데 이것들을 다 MonoBehaviour로 둬도 되는 것인가? MonoBehaviour가 정확히 뭘까?
 * 쿨타임 계산의 용이함과 요즘 기기의 스펙을 고려하면 Empty에 넣어두는 건 어렵지 않음
 * PlayerAttackController와도 연동이 잘되니 실제 안심
 * 그런데 그러면 PlayerAlive는 너무 많은 SerializedField를 갖고, 너무 많은 객체에 정보를 전달해야 한다
 * Stats의 경우에도 BaseStat만 전달하면 되지만 그 양이 은근히 많음
*/