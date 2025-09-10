using SOInterfaces;
using UnityEngine;

public class SkillRunner : MonoBehaviour, ISkillRunner
{
    ISkillMechanic mech;
    ISkillParam param;
    Camera cam;
    bool busy; float cd;

    public bool IsBusy => busy;
    public bool IsOnCooldown => cd > 0f;

    public void Init(ISkillMechanic mechanic, ISkillParam param)
    {
        this.mech = mechanic;
        this.param = param;
        this.cam = Camera.main;
    }

    void Update() { if (cd > 0f) cd -= Time.deltaTime; }

    public void TryCast()
    {
        if (busy || IsOnCooldown || mech == null || param == null) return;
        StartCoroutine(CoCast());
    }

    System.Collections.IEnumerator CoCast()
    {
        busy = true;
        // ��Ŀ������ ������ �ڷ�ƾ ����
        yield return mech.Cast(transform, cam, param);

        // ��ٿ��� �Ķ���Ͱ� ������ ��츸 ����
        if (param is IHasCooldown hasCd) cd = hasCd.Cooldown;

        busy = false;
    }
}