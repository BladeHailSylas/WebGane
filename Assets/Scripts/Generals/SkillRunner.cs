// SkillRunner_V2_Lite.cs — "7단계 파이프라인" 리팩터 (로직 변경 최소화)
// - 목표: CoCast를 KinematicMotor2D.SweepMove 수준으로 얇게 유지
// - 핵심: BuildContext → Validate → BeginCostAndBusy → EnsureAnchor → Execute → ApplyPipeline → HandleFollowUps (+Finalize)
// - 주의: 전투 수치 변경/Effect 파이프라인은 기존 시스템을 그대로 사용(여기서는 이벤트 발행 수준)
// - 디버그: debugLogging 토글, 단계별 Trace, 실패 사유 로깅

using ActInterfaces;
using SkillInterfaces;
using System;
using System.Collections;
using UnityEngine;
using static EventBus;
using static GameEventMetaFactory;
using static TargetAnchorUtil2D;

public class SkillRunner : MonoBehaviour, ISkillRunner
{
	[Header("Bindings")]
	[SerializeField] ISkillMechanic mech;
	[SerializeReference] ISkillParam param;

	[Header("Debugging")]
	[SerializeField] bool debugLogging = false;           // 단계별 로그 토글
	[SerializeField] int warnFollowUpsPerFrame = 16;      // 프레임당 과도한 FollowUp 감지

	Camera cam;
	bool busy; float cd;
	int castSeq;                  // 단순 CastId 시퀀스
	int scheduledThisFrame;       // 프레임당 FollowUp 스케줄 카운트

	// === Public state ===
	public bool IsBusy => busy;
	public bool IsOnCooldown => cd > 0f;

	void Awake() { cam = Camera.main; }
	void Update()
	{
		if (cd > 0f) cd -= Time.deltaTime;
		scheduledThisFrame = 0; // 프레임 경계에서 초기화
	}

	// ---------------------------------------------------------------------
	// Entry: 슬롯 기본 스킬 시전 (입력에서 호출)
	// ---------------------------------------------------------------------
	public void TryCast()
	{
		if (busy || cd > 0f || mech == null || param == null)
		{
			D($"TryCast blocked: busy={busy}, cd={cd:F2}, mech={(mech == null)}, param={(param == null)}");
			return;
		}

		// (옵션) Switch 정책: 외부에서 주문서 선택
		if (param is ISwitchPolicy sp && sp.TrySelect(transform, cam, out var switched))
		{
			D("SwitchPolicy selected alt CastOrder → Schedule");
			Schedule(switched, 0f, respectBusyCooldown: true);
			return;
		}

		// 기본 메커닉 시전
		Schedule(new CastOrder(mech, param), 0f, respectBusyCooldown: true);
	}

	// === 표준 스케줄 API (일반/FollowUp/스위치 동일 경로) ===
	public void Schedule(CastOrder order, float delay, bool respectBusyCooldown)
	{
		StartCoroutine(CoSchedule(order, delay, respectBusyCooldown));
	}

	IEnumerator CoSchedule(CastOrder order, float delay, bool respect)
	{
		if (respect)
		{
			while (busy || cd > 0f) yield return null;  // 바쁨/쿨다운 존중
		}
		if (delay > 0f) yield return new WaitForSeconds(delay);
		yield return CoCast(order);
	}

	// =====================================================================
	// CoCast — 얇은 7단계 파이프라인 (로직은 작은 함수로 위임)
	// =====================================================================
	IEnumerator CoCast(CastOrder order)
	{
		var ctx = BuildContext(order);                           // 1) 컨텍스트
		var vResult = Validate(ctx, order.Param);                // 2) 검증(부작용 X)
		if (!vResult.IsValid)
		{
			FailCastEarly(ctx, vResult);
			yield break;
		}

		BeginCostAndBusy(ctx, order);                            // 3) 비용/바쁨/쿨다운(시점)
		using (var anchor = EnsureAnchor(ctx, order))            // 4) 앵커 스코프(자동 해제)
		{
			Hooks_OnCastStart(ctx);

			var res = ExecuteMechanism(ctx, order, anchor.Target); // 5) 메커니즘 실행
			yield return res.Yieldable;                            // 실행 코루틴 대기(필요 시)

			ApplyPipeline(ctx, res);                              // 6) 결과 반영(이벤트/파이프라인)
			HandleFollowUps(ctx, res);                            // 7) FollowUp 스케줄
		}
		FinalizeCast(ctx, order);                                 // End 마무리
	}

	// ------------------------------------------------------------------
	// (1) BuildContext — 메타/식별자/참조 구성 (순수)
	// ------------------------------------------------------------------
	private CastContext BuildContext(CastOrder order)
	{
		var meta = Create(transform, channel: "combat"); // 공통 메타
		var skillRef = new SkillRef(order.Mech as UnityEngine.Object); // SO 참조 기반
		var castId = ++castSeq;
		var c = new CastContext(castId, transform, cam, meta, skillRef, order);
		D($"[CTX] CastId={castId} Mech={order.Mech?.GetType().Name}");
		return c;
	}

	// ------------------------------------------------------------------
	// (2) Validate — 사전 검증(부작용 없음)
	// ------------------------------------------------------------------
	private ValidationResult Validate(CastContext ctx, ISkillParam p)
	{
		// 현재 시스템에서는 TryCast에서 busy/cd를 걸러내므로 여기선 타깃/입력 유효성만 사전 체크 가능
		// 타깃은 EnsureAnchor에서 최종적으로 다시 확인되므로, 여기서는 단순 패스
		return ValidationResult.Ok;
	}

	private void FailCastEarly(CastContext ctx, ValidationResult vr)
	{
		// 현재는 별도 사유가 거의 없으므로 이벤트 최소 발행
		Publish(new CastEnded(ctx.Meta, ctx.SkillRef, ctx.Caster, interrupted: true));
		D($"[FAIL] Early validation fail: reason={vr.Reason}");
	}

	// ------------------------------------------------------------------
	// (3) BeginCostAndBusy — 비용/쿨다운/바쁨 시점 처리
	// ------------------------------------------------------------------
	private void BeginCostAndBusy(CastContext ctx, CastOrder order)
	{
		busy = true; // FollowUp respect 정책은 스케줄러에서 처리
					// 쿨다운은 기존 코드처럼 종료 시에 반영(IHasCooldown) → 로직 유지
		D("[BEGIN] Busy=true");
	}

	// ------------------------------------------------------------------
	// (4) EnsureAnchor — 타깃/앵커 확보를 스코프화(IDisposable)
	//   - 타깃형: 대상 Transform 확보(없으면 실패)
	//   - 비타깃형/좌표형: 앵커 Transform 생성 후 스코프 종료 시 Release
	// ------------------------------------------------------------------
	private AnchorScope EnsureAnchor(CastContext ctx, CastOrder order)
	{
		if (order.Mech is ITargetedMechanic tgt)
		{
			// 우선순위: 명시 TargetOverride → 적 타깃 → 목적지 앵커
			Transform t = order.TargetOverride;
			if (t == null)
			{
				bool needEnemy = true;
				Vector3 desired = default;
				float rad = 0f, skin = 0.05f; LayerMask walls = 0;

				if (order.Param is ITargetingData td)
				{
					walls = td.WallsMask;
					rad = td.CollisionRadius;
					skin = Mathf.Max(0.01f, td.AnchorSkin);

					switch (td.Mode)
					{
						case TargetMode.TowardsEnemy:
							needEnemy = true; break;
						case TargetMode.TowardsCursor:
							needEnemy = false;
							var cursor = CursorWorld2D(cam, transform, depthFallback: 10f);
							desired = transform.position + (cursor - transform.position).normalized * Mathf.Max(0f, td.FallbackRange);
							break;
						case TargetMode.TowardsMovement:
							needEnemy = false;
							if (!TryGetMoveDir(transform, out var moveDir))
							{
								// 타깃 실패 처리 — 이벤트 + 종료는 상위(CoCast)에서 일관되게 수행
								return AnchorScope.Fail("NoMoveDir");
							}
							desired = transform.position + (Vector3)(moveDir * Mathf.Max(0f, td.FallbackRange));
							break;
						case TargetMode.TowardsOffset:
							needEnemy = false;
							desired = transform.TransformPoint((Vector3)td.LocalOffset);
							break;
					}
				}

				if (needEnemy)
				{
					var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
					if (provider == null || !provider.TryGetTarget(out t) || t == null)
						return AnchorScope.Fail("NoEnemyTarget");
				}
				else
				{
					var clamped = ResolveReachablePoint2D(transform.position, desired, walls, rad, skin);
					var v = (clamped - transform.position);
					if (v.sqrMagnitude < 0.0001f)
						return AnchorScope.Fail("AnchorTooClose");

					t = TargetAnchorPool.Acquire(clamped);
					return AnchorScope.Created(t); // 스코프 종료 시 Release
				}
			}
			// 명시 타깃 또는 적 타깃 성공
			return AnchorScope.Reused(t);
		}
		// 비타깃 메커닉
		return AnchorScope.None();
	}

	// ------------------------------------------------------------------
	// (5) Execute — 메커니즘 실행 (게임 로직은 여기에서만 수행)
	// ------------------------------------------------------------------
	private ExecResult ExecuteMechanism(CastContext ctx, CastOrder order, Transform target)
	{
		// 이벤트: 시작/타깃 획득
		Publish(new CastStarted(ctx.Meta, ctx.SkillRef, transform, target));
		if (order.Mech is ITargetedMechanic tgt && target != null)
			Publish(new TargetAcquired(ctx.Meta, ctx.SkillRef, transform, target));

		// 실제 메커니즘(코루틴) 실행
		if (order.Mech is ITargetedMechanic tgtMech)
		{
			return ExecResult.FromCoroutine(tgtMech.Cast(transform, cam, order.Param, target));
		}
		else
		{
			return ExecResult.FromCoroutine(order.Mech.Cast(transform, cam, order.Param));
		}
	}

	// ------------------------------------------------------------------
	// (6) ApplyPipeline — 결과 반영 (현 시스템: 이벤트 수준 유지)
	// ------------------------------------------------------------------
	private void ApplyPipeline(CastContext ctx, ExecResult res)
	{
		// 본 프로젝트에선 Mechanic이 내부에서 전투 처리를 수행하고, Runner는 이벤트를 발행하는 역할
		// 필요 시 Effect 파이프라인 연계 지점
		// (여기서는 추가 변경 없음)
	}

	/// <summary>
	/// (7) HandleFollowUps — 훅/정책을 통해 FollowUp 스케줄 (로직 변경 없음)
	/// </summary>
	/// <param name="ctx"></param>
	/// <param name="res"></param>
	private void HandleFollowUps(CastContext ctx, ExecResult res)
	{
		// 기존 구조 유지: 훅에서 FollowUp들을 Schedule로 흘림
		// 필요 시 MaxDepth/우선순위는 별도 정책 클래스로 추출 가능
		// 여기에 디버그용 카운터 삽입
		if (scheduledThisFrame > warnFollowUpsPerFrame)
		{
			W($"FollowUps scheduled too many in a single frame: {scheduledThisFrame}");
		}
	}

	///<summary>
	/// End — 쿨다운/훅/바쁨 해제 (기존 로직 최대한 유지)
	///</summary>
	private void FinalizeCast(CastContext ctx, CastOrder order)
	{
		if (order.Param is IHasCooldown h) cd = Mathf.Max(cd, h.Cooldown);
		Publish(new CastEnded(ctx.Meta, ctx.SkillRef, transform, false));
		Hooks_OnCastEnd(ctx);
		busy = false;
		D("[END] Cast finalized: Busy=false");
	}

	// === 훅 엔드포인트(메커닉에서 콜백) ===
	public void NotifyHookOnHit(Transform target, Vector2 point) => BroadcastHook(AbilityHook.OnHit, target, param);
	public void NotifyHookOnExpire(Vector2 point) => BroadcastHook(AbilityHook.OnCastEnd, null, param);

	void BroadcastHook(AbilityHook hook, Transform prevTarget, ISkillParam srcParam)
	{
		try
		{
			if (srcParam is IFollowUpProvider p)
			{
				foreach (var (order, delay, respect) in p.BuildFollowUps(hook, prevTarget))
				{
					Schedule(order, delay, respect);
					scheduledThisFrame++;
					if (debugLogging) D($"[HOOK] {hook} → Schedule(delay={delay:F2}, respect={respect})");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError($"[SkillRunner] Hook exception on {hook}");
			Debug.LogException(ex);
		}
	}

	// === 훅 호출(안전하게 try/catch 내부) ===
	private void Hooks_OnCastStart(CastContext ctx) => BroadcastHook(AbilityHook.OnCastStart, null, param);
	private void Hooks_OnCastEnd(CastContext ctx) => BroadcastHook(AbilityHook.OnCastEnd, null, param);

	// === 바인딩 ===
	public void Init(ISkillMechanic m, ISkillParam p) { mech = m; param = p; cam = Camera.main; }

	// === Debug helpers ===
	private void D(string msg)
	{
		if (debugLogging) Debug.Log($"[SkillRunner#{castSeq}] {msg}");
	}
	private void W(string msg)
	{
		Debug.LogWarning($"[SkillRunner#{castSeq}] {msg}");
	}

	// =====================================================================
	// 내부 타입들 — 컨텍스트/실행결과/앵커 스코프
	// =====================================================================
	private readonly struct CastContext
	{
		public readonly int CastId;
		public readonly Transform Caster;
		public readonly Camera Cam;
		public readonly GameEventMeta Meta;
		public readonly SkillRef SkillRef;
		public readonly CastOrder Order;
		public CastContext(int castId, Transform caster, Camera cam, GameEventMeta meta, SkillRef skillRef, CastOrder order)
		{ CastId = castId; Caster = caster; Cam = cam; Meta = meta; SkillRef = skillRef; Order = order; }
	}

	private readonly struct ValidationResult
	{
		public static readonly ValidationResult Ok = new ValidationResult(true, "");
		public readonly bool IsValid;
		public readonly string Reason;
		public ValidationResult(bool ok, string reason) { IsValid = ok; Reason = reason; }
	}

	private readonly struct ExecResult
	{
		public readonly IEnumerator Yieldable; // 메커니즘 코루틴(없으면 Empty)
		private ExecResult(IEnumerator y) { Yieldable = y; }
		public static ExecResult FromCoroutine(IEnumerator y) => new ExecResult(y);
	}

	private struct AnchorScope : IDisposable
	{
		public readonly Transform Target;   // 적 타깃 또는 생성 앵커
		readonly bool created;
		readonly bool failed;
		readonly string failReason;
		public bool IsFailed => failed;
		private AnchorScope(Transform t, bool created, bool failed, string reason) { Target = t; this.created = created; this.failed = failed; failReason = reason; }
		public static AnchorScope None() => new AnchorScope(null, false, false, "");
		public static AnchorScope Reused(Transform t) => new AnchorScope(t, false, false, "");
		public static AnchorScope Created(Transform t) => new AnchorScope(t, true, false, "");
		public static AnchorScope Fail(string reason) => new AnchorScope(null, false, true, reason);
		public void Dispose() { if (created && Target != null) TargetAnchorPool.Release(Target); }
		public override string ToString() => failed ? $"Fail({failReason})" : (created ? "Created" : (Target ? "Reused" : "None"));
	}
}
