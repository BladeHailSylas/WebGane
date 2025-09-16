// Ŀ�� ���¸� �����ϴ� �ʼ��� ������Ʈ
using UnityEngine;

public class SwitchCursorState : MonoBehaviour //CursorState�� �ʿ���? ��?

{
    int _idx = -1;

    public int GetAndAdvance(int count, int startIndex, bool advanceNow)
    {
        if (count <= 0) return 0;
        if (_idx < 0) _idx = Mathf.Clamp(startIndex, 0, count - 1);
        int cur = _idx;
        if (advanceNow) _idx = (_idx + 1) % count;
        return cur;
    }

    // OnHit���� ���� ������ �ʿ��� �� ȣ��
    public void AdvanceExplicit(int count) => _idx = (_idx + 1 + count) % count;
}