using CharacterSOInterfaces;
using UnityEngine;

public class SkillRunner : MonoBehaviour, ISkillRunner
{
    ISkillMechanic mech;
    ISkillParam param;
    Camera cam;
    bool busy;
    float cd;

    public bool IsBusy => busy;
    public bool IsOnCooldown => cd > 0f;

    public void Init(ISkillMechanic mechanic, ISkillParam param)
    {
        this.mech = mechanic;
        this.param = param;
        cam = Camera.main;
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
        // 메커니즘이 제공하는 "캐스팅 코루틴"을 그대로 실행
        yield return mech.Cast(transform, cam, param);

        // 쿨다운은 파라미터가 갖고 있거나(공통 인터페이스) 코루틴 내부에서 반환값으로 넘겨도 OK
        if (param is IHasCooldown hasCd) cd = hasCd.Cooldown;

        busy = false;
    }
}

// (선택) 공통 쿨다운 인터페이스
public interface IHasCooldown : ISkillParam { float Cooldown { get; } }