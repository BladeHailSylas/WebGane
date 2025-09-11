using SOInterfaces;
using ActInterfaces;
//using UnityEditor.ShaderGraph.Configuration;
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
    { mech = mechanic; this.param = param; cam = Camera.main; }

    public void TryCast()
    {
        if (busy || cd > 0f || mech == null || param == null) return;
        StartCoroutine(CoCast());
    }

    System.Collections.IEnumerator CoCast()
    {
        busy = true;

        // ① 타깃형이면 커서 타깃을 구해 전달
        if (mech is ITargetedMechanic tgtMech)
        {
            var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
            if (provider == null || !provider.TryGetTarget(out Transform target) || target == null)
            {
                busy = false; yield break; // 타깃 없으면 시전 취소(정책에 따라 실패 사운드 등)
            }
            yield return tgtMech.Cast(transform, cam, param, target);
        }
        else
        {
            // ② 일반 스킬은 기존 코루틴 사용
            yield return mech.Cast(transform, cam, param);
        }

        if (param is IHasCooldown hasCd) cd = hasCd.Cooldown;
        busy = false;
    }

    void Update() { if (cd > 0f) cd -= Time.deltaTime; }
}