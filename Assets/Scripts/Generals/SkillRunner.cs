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
        // 메커니즘이 제공한 코루틴 실행
        yield return mech.Cast(transform, cam, param);

        // 쿨다운은 파라미터가 구현한 경우만 적용
        if (param is IHasCooldown hasCd) cd = hasCd.Cooldown;

        busy = false;
    }
}