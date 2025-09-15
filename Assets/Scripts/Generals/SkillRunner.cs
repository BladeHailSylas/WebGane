// SkillRunner.cs (추가/수정)
using SOInterfaces;
using ActInterfaces;
using UnityEngine;
using System.Collections;

// ★ CharacterSpec.FollowUpBinding 사용을 위해 using 추가
using static CharacterSpec;

public class SkillRunner : MonoBehaviour, ISkillRunner
{
    ISkillMechanic mech;
    ISkillParam param;
    Camera cam;
    bool busy; float cd;

    public bool IsBusy => busy;
    public bool IsOnCooldown => cd > 0f;

    // ★ 캐릭터별 콤보 바인딩 저장
    FollowUpBinding _onCastStart, _onHit, _onExpire;

    // 기존 Init 확장: 훅별 follow-up을 함께 주입
    public void Init(ISkillMechanic mechanic, ISkillParam param,
                    FollowUpBinding onCastStart = default,
                    FollowUpBinding onHit = default,
                    FollowUpBinding onExpire = default)
    {
        mech = mechanic; this.param = param; cam = Camera.main;
        _onCastStart = onCastStart; _onHit = onHit; _onExpire = onExpire;
    }

    public void TryCast()
    {
        if (busy || cd > 0f || mech == null || param == null) return;
        StartCoroutine(CoCast());
    }

    System.Collections.IEnumerator CoCast()
    {
        busy = true;

        // ★ 시전 시작 훅 follow-up
        TryFollowUp(_onCastStart, null, default);

        // 기존 로직 유지 (타깃형/일반 분기)
        if (mech is ITargetedMechanic tgtMech)
        {
            var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
            if (provider == null || !provider.TryGetTarget(out Transform target) || target == null)
            { busy = false; yield break; }
            yield return tgtMech.Cast(transform, cam, param, target);
        }
        else
        {
            yield return mech.Cast(transform, cam, param);
        }

        if (param is IHasCooldown hasCd) cd = hasCd.Cooldown;
        busy = false;
    }

    void Update() { if (cd > 0f) cd -= Time.deltaTime; }

    // ★ 메커닉에서 훅 통지 시 호출
    public void NotifyHookOnHit(Transform target, Vector2 point) => TryFollowUp(_onHit, target, point);
    public void NotifyHookOnExpire(Vector2 point) => TryFollowUp(_onExpire, null, point);

    // ★ follow-up 실행 공통 처리
    void TryFollowUp(FollowUpBinding fu, Transform tgt, Vector2 hitPoint)
    {
        if (fu.mechanic is not ISkillMechanic) return;
        if (!fu.IsValid(out ISkillMechanic next)) return;

        StartCoroutine(Co());
        IEnumerator Co()
        {
            if (fu.respectBusyCooldown && (busy || cd > 0f)) yield break;
            if (fu.delay > 0f) yield return new WaitForSeconds(fu.delay);

            if (next is ITargetedMechanic tnext)
            {
                // 타깃형 follow-up: 이전 타깃 전달 또는 새로 조달
                Transform pass = fu.passSameTarget ? tgt : null;
                if (pass == null)
                {
                    var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
                    if (provider == null || !provider.TryGetTarget(out pass) || pass == null) yield break;
                }
                yield return tnext.Cast(transform, cam, fu.param, pass);
            }
            else
            {
                // 일반형 follow-up
                yield return next.Cast(transform, cam, fu.param);
            }

            // follow-up 쿨타임 반영(있다면)
            if (fu.param is IHasCooldown h) cd = Mathf.Max(cd, h.Cooldown);
        }
    }
}
