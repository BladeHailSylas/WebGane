// SkillRunner_IntentIntegrated.cs — Intent 기반 Root/FollowUp 일반화 (로직 변경 최소화)
// - 기존 7단계(CoCast) 파이프라인은 유지하고, 그 "앞단"에 Intent 큐를 추가합니다.
// - TryCast와 FollowUp 훅은 이제 Intent를 생성해 큐에 넣고, FixedUpdate에서 소비합니다.
// - Intent는 CastOrder를 감싼 부작용 없는 데이터로, Runner만이 이를 실행(스케줄)합니다.
//
// 주의: 본 파일은 사용 중인 프로젝트 인터페이스를 그대로 활용하도록 구성했습니다.
//       (ISkillMechanic/ISkillParam/IFollowUpProvider/ITargetedMechanic 등)
//       컴파일러 에러가 나면 네임스페이스 혹은 using을 프로젝트에 맞게 조정하세요.

using ActInterfaces;
using SkillInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EventBus;
using static GameEventMetaFactory;
using static TargetAnchorUtil2D;
using static UnityEngine.GraphicsBuffer;

public class SkillRunner : MonoBehaviour, ISkillRunner
{
	#region ===== Settings =====
	[Header("Bindings")]
	[SerializeField] ISkillMechanic mech;
	[SerializeReference] ISkillParam param;

	[Header("Debugging")]
	[SerializeField] bool debugLogging = false;           // 단계별 로그 토글
	[SerializeField] int warnFollowUpsPerFrame = 16;      // 프레임당 과도한 FollowUp 감지
	int frameRate = 60;                             // 프레임레이트
	Camera cam;
	bool busy; float cd;
	int castSeq;                  // 단순 CastId 시퀀스(루트 Intent에 부여)
	int scheduledThisFrame;       // 프레임당 FollowUp 스케줄 카운트

	// === Public properties ===
	public bool IsBusy => busy;
	public bool IsOnCooldown => cd > 0f;

	void Awake() { cam = Camera.main; }

	void Update()
	{
		if (cd > 0f) cd -= 1.0f / frameRate;
		scheduledThisFrame = 0; // 프레임 경계에서 초기화
	}
	#endregion

	// ---------------------------------------------------------------------
	// Entry: 외부(입력/AI)에서 호출 — 이제 루트도 Intent로 일반화
	// ---------------------------------------------------------------------
	public void TryCast()
	{
		if (mech == null || param == null)
		{
			DLog($"TryCast blocked: mech={(mech == null)}, param={(param == null)}");
			return;
		}

		// 스위치 정책이 있으면 먼저 해석
		if (param is ISwitchPolicy sp && sp.TrySelect(transform, cam, out var switched))
		{
			EnqueueRootIntent(switched.Mech, switched.Param, respectBusyCooldown: true, delaySeconds: 0f);
			return;
		}

		// 기본 시전: Root Intent로 큐잉(즉시 실행 금지 → FixedUpdate에서 결정적 소비)
		EnqueueRootIntent(mech, param, respectBusyCooldown: true, delaySeconds: 0f);
	}

	// === 기존 스케줄 API(호환용): Intent 경유로 전환 ===
	public void Schedule(CastOrder order, float delay, bool respectBusyCooldown)
	{
		if (order.Mech == null || order.Param == null) { DLog("Schedule ignored: null order"); return; }
		// FollowUp도 Intent로 변환하여 같은 경로로 흘려보냄
		var intent = CastIntent.FollowUp(
			parentCastId: 0, // 알 수 없을 때 0; 실제 실행 시 BuildContext에서 새로운 CastId 부여
			depth: 1,
			sourceHook: "Schedule",
			templateId: "compat",
			order.Mech,
			order.Param,
			timing: IntentTiming.Delayed,
			delaySeconds: Mathf.Max(0f, delay),
			respectBusyCooldown: respectBusyCooldown
		);
		_intentQueue.Enqueue(intent);
		DLog($"[Compat→Intent] Schedule → {intent}");
	}

	// =====================================================================
	// Intent 소비 루프 — FixedUpdate에서만 꺼내 CoCast로 실행(7단계 유지)
	// =====================================================================
	void FixedUpdate()
	{
		int processed = 0;
		int budget = PerTickBudget;
		while (budget-- > 0 && _intentQueue.Count > 0)
		{
			var intent = _intentQueue.Dequeue();
			if (!TryConsumeIntent(intent)) break; // 소비 중 실행 예약(코루틴) — 실패 시 탈출
			processed++;
		}
		if (debugLogging && processed > 0) Debug.Log($"[IntentTick] processed={processed} remain={_intentQueue.Count}");
	}

	bool TryConsumeIntent(CastIntent i)
	{
		// Busy/Cooldown 존중 여부 처리(루트/팔로우업 공통)
		if (i.RespectBusyCooldown && (busy || cd > 0f))
		{
			// 재시도: 같은 Intent를 다음 틱에 재큐잉(간단 구현)
			_intentQueue.Enqueue(i);
			return true;
		}

		// 딜레이 처리 — IntentTiming.Delayed를 초 단위로 변환하여 코루틴 사용
		if (i.Timing == IntentTiming.Delayed && i.DelaySeconds > 0f)
		{
			StartCoroutine(CoDelayedConsume(i));
			return true;
		}

		// 실제 실행 경로: Intent → CastOrder로 감싸 기존 CoCast 활용
		var order = new CastOrder(i.Mechanism, i.Param, i.TargetOverride);
		StartCoroutine(CoCast(order));
		return true;
	}

	IEnumerator CoDelayedConsume(CastIntent i)
	{
		// Busy/CD 존중 의도는 딜레이 전에도 체크했지만, 딜레이 후에도 다시 체크
		if (i.RespectBusyCooldown)
		{
			while (busy || cd > 0f) yield return null;
		}
		yield return new WaitForSeconds(i.DelaySeconds);
		var order = new CastOrder(i.Mechanism, i.Param, i.TargetOverride);
		yield return CoCast(order);
	}

	// =====================================================================
	// 기존 7단계(CoCast) 파이프라인 — 변경 없음 (일부 로그만 개선)
	// =====================================================================
	IEnumerator CoCast(CastOrder order)
	{
		var ctx = BuildContext(order);                           // 1) 컨텍스트
		var vResult = Validate(ctx, order.Param);                // 2) 검증(부작용 X)
		if (!vResult.IsValid) { FailCastEarly(ctx, vResult); yield break; }

		BeginCostAndBusy(ctx, order);                            // 3)
		using (var anchor = EnsureAnchor(ctx, order))            // 4)
		{
			Hooks_OnCastStart(ctx);
			var res = ExecuteMechanism(ctx, order, anchor.Target); // 5)
			yield return res.Yieldable;                            // 필요 시 대기
			ApplyPipeline(ctx, res);                              // 6)
			HandleFollowUps(ctx, res);                            // 7) (호환: IFollowUpProvider 기반)
		}
		FinalizeCast(ctx, order);                                 // End
	}

	#region ===== Methods =====
	private CastContext BuildContext(CastOrder order)
	{
		var meta = Create(transform, channel: "combat");
		var skillRef = new SkillRef(order.Mech as UnityEngine.Object);
		var castId = ++castSeq; // Intent로도 들어오지만 CoCast 기준으로 새 ID 부여(로그용)
		var c = new CastContext(castId, 0, transform, cam, meta, skillRef, order);
		DLog($"[CTX] CastId={castId} Mech={order.Mech?.GetType().Name}");
		return c;
	}

	private ValidationResult Validate(CastContext ctx, ISkillParam p) => ValidationResult.Ok;

	private void FailCastEarly(CastContext ctx, ValidationResult vr)
	{ Publish(new CastEnded(ctx.Meta, ctx.SkillRef, transform, interrupted: true)); DLog($"[FAIL] {vr.Reason}"); }

	private void BeginCostAndBusy(CastContext ctx, CastOrder order)
	{ busy = true; DLog("[BEGIN] Busy=true"); }

	private AnchorScope EnsureAnchor(CastContext ctx, CastOrder order)
	{
		if (order.Mech is ITargetedMechanic tgt)
		{
			Transform t = order.TargetOverride;
			if (t == null)
			{
				bool needEnemy = true; Vector3 desired = default; float rad = 0f, skin = 0.05f; LayerMask walls = 0;
				if (order.Param is ITargetingData td)
				{
					walls = td.WallsMask; rad = td.CollisionRadius; skin = Mathf.Max(0.01f, td.AnchorSkin);
					switch (td.Mode)
					{
						case TargetMode.TowardsEnemy: needEnemy = true; break;
						case TargetMode.TowardsCursor: needEnemy = false; var cursor = CursorWorld2D(cam, transform, 10f); desired = transform.position + (cursor - transform.position).normalized * Mathf.Max(0f, td.FallbackRange); break;
						case TargetMode.TowardsMovement: needEnemy = false; if (!TryGetMoveDir(transform, out var moveDir)) return AnchorScope.Fail("NoMoveDir"); desired = transform.position + (Vector3)(moveDir * Mathf.Max(0f, td.FallbackRange)); break;
						case TargetMode.TowardsOffset: needEnemy = false; desired = transform.TransformPoint((Vector3)td.LocalOffset); break;
					}
				}
				if (needEnemy)
				{ var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>(); if (provider == null || !provider.TryGetTarget(out t) || t == null) return AnchorScope.Fail("NoEnemyTarget"); }
				else
				{ var clamped = ResolveReachablePoint2D(transform.position, desired, walls, rad, skin); var v = (clamped - transform.position); if (v.sqrMagnitude < 0.0001f) return AnchorScope.Fail("AnchorTooClose"); t = TargetAnchorPool.Acquire(clamped); return AnchorScope.Created(t); }
			}
			return AnchorScope.Reused(t);
		}
		return AnchorScope.None();
	}

	private ExecResult ExecuteMechanism(CastContext ctx, CastOrder order, Transform target)
	{
		Publish(new CastStarted(ctx.Meta, ctx.SkillRef, ctx.Order.Param, transform, target));
		if (order.Mech is ITargetedMechanic && target != null)
			Publish(new TargetAcquired(ctx.Meta, ctx.SkillRef, transform, target));

		if (order.Mech is ITargetedMechanic tgtMech)
			return ExecResult.FromCoroutine(tgtMech.Cast(transform, cam, order.Param, target));
		else
			return ExecResult.FromCoroutine(order.Mech.Cast(transform, cam, order.Param));
	}

	private void ApplyPipeline(CastContext ctx, ExecResult res) { /* 전투 파이프라인 연계 지점(현상 유지) */ }

	private void HandleFollowUps(CastContext ctx, ExecResult res)
	{
		// 기존: Param(IFollowUpProvider) → CastOrder,delay,respect 생성 → Schedule 호출
		// 변경: Schedule을 Intent로 감싸므로 아래 BroadcastHook가 Intent enq를 수행함
		if (scheduledThisFrame > warnFollowUpsPerFrame * 2) ELog($"FollowUps scheduled unpardonably in a single frame: {scheduledThisFrame}");
		else if (scheduledThisFrame > warnFollowUpsPerFrame) WLog($"FollowUps scheduled too many in a single frame: {scheduledThisFrame}");
	}

	private void FinalizeCast(CastContext ctx, CastOrder order)
	{
		if (order.Param is IHasCooldown h) cd = Mathf.Max(cd, h.Cooldown);
		Publish(new CastEnded(ctx.Meta, ctx.SkillRef, transform, false));
		Hooks_OnCastEnd(ctx);
		busy = false; DLog("[END] Cast finalized: Busy=false");
	}

	// === 훅 엔드포인트(메커닉에서 콜백) ===
	public void NotifyHook(AbilityHook hook, ISkillParam currentParam, Transform target = null) => BroadcastHook(hook, target, currentParam, 0);
	private void Hooks_OnCastStart(CastContext ctx) => BroadcastHook(AbilityHook.OnCastStart, null, ctx.Order.Param, ctx.ChainDepth);
	private void Hooks_OnCastEnd(CastContext ctx) => BroadcastHook(AbilityHook.OnCastEnd, null, ctx.Order.Param, ctx.ChainDepth);
	void BroadcastHook(AbilityHook hook, Transform prevTarget, ISkillParam srcParam, int prevChain)
	{
		try
		{
			if (srcParam is IFollowUpProvider p)
			{
				foreach (var (order, delay, respect) in p.BuildFollowUps(hook, prevTarget))
				{
					var intent = CastIntent.FollowUp(
						parentCastId: castSeq,
						depth: prevChain + 1,
						sourceHook: hook.ToString(),
						templateId: "param.tpl",
						order.Mech, order.Param,
						timing: delay > 0 ? IntentTiming.Delayed : IntentTiming.Immediate,
						delaySeconds: Mathf.Max(0f, delay),
						respectBusyCooldown: respect,
						targetOverride: order.TargetOverride
					);
					_intentQueue.Enqueue(intent);
					scheduledThisFrame++;
					if (debugLogging) DLog($"[HOOK→Intent] {hook} → {intent}");
				}
			}
		}
		catch (Exception ex) { ELog($"[SkillRunner] Hook exception on {hook}"); Debug.LogException(ex); }
	}
	public void Init(ISkillMechanic m, ISkillParam p) { mech = m; param = p; cam = Camera.main; }

	private void DLog(string msg) { if (debugLogging) Debug.Log($"[SkillRunner#{castSeq}] {msg}"); }
	private void WLog(string msg) { if (debugLogging) Debug.LogWarning($"[SkillRunner#{castSeq}] ? {msg}"); }
	private void ELog(string msg) { if (debugLogging) Debug.LogError($"[SkillRunner#{castSeq}] ! {msg}"); }

	// =====================================================================
	// 내부 타입들 — 컨텍스트/실행결과/앵커 스코프 (기존 유지)
	// =====================================================================
	private readonly struct CastContext
	{
		public readonly int CastId; public readonly int ChainDepth; public readonly Transform Caster; public readonly Camera Cam; public readonly GameEventMeta Meta; public readonly SkillRef SkillRef; public readonly CastOrder Order;
		public CastContext(int castId, int chainDepth, Transform caster, Camera cam, GameEventMeta meta, SkillRef skillRef, CastOrder order) { CastId = castId; ChainDepth = chainDepth; Caster = caster; Cam = cam; Meta = meta; SkillRef = skillRef; Order = order; }
	}

	private readonly struct ValidationResult { public static readonly ValidationResult Ok = new(true, ""); public readonly bool IsValid; public readonly string Reason; public ValidationResult(bool ok, string reason) { IsValid = ok; Reason = reason; } }

	private readonly struct ExecResult { public readonly IEnumerator Yieldable; private ExecResult(IEnumerator y) { Yieldable = y; } public static ExecResult FromCoroutine(IEnumerator y) => new(y); }

	private readonly struct AnchorScope : IDisposable
	{
		public readonly Transform Target; readonly bool created; readonly bool failed; readonly string failReason; public bool IsFailed => failed;
		private AnchorScope(Transform t, bool created, bool failed, string reason) { Target = t; this.created = created; this.failed = failed; failReason = reason; }
		public static AnchorScope None() => new(null, false, false, "");
		public static AnchorScope Reused(Transform t) => new(t, false, false, "");
		public static AnchorScope Created(Transform t) => new(t, true, false, "");
		public static AnchorScope Fail(string reason) => new(null, false, true, reason);
		public void Dispose() { if (created && Target != null) TargetAnchorPool.Release(Target); }
		public override string ToString() => failed ? $"Fail({failReason})" : (created ? "Created" : (Target ? "Reused" : "None"));
	}

	#endregion

	// =====================================================================
	// #region IntentSetting — Intent 핵심 설정/설명 묶음 (이 파일 하단)
	// =====================================================================
	#region IntentSetting

	// 1) 성능/정책
	[Header("Intent/Policy")]
	[SerializeField] int _perTickBudget = 64;   // 틱당 Intent 소비 상한(백프레셔)
	public int PerTickBudget => _perTickBudget;

	// 2) Intent 큐(루트/팔로우업 공용)
	readonly Queue<CastIntent> _intentQueue = new();

	// 3) Intent 생성 헬퍼 — 루트 전용
	public void EnqueueRootIntent(ISkillMechanic m, ISkillParam p, bool respectBusyCooldown, float delaySeconds = 0f, Transform targetOverride = null)
	{
		var intent = CastIntent.Root(++castSeq, m, p, respectBusyCooldown, delaySeconds, targetOverride);
		_intentQueue.Enqueue(intent);
		DLog($"[RootIntent+] {intent}");
	}

	// === Intent 자료형(최소 필수 필드) ===
	public enum IntentOrigin { Root, FollowUp }
	public enum IntentTiming { Immediate, Delayed }

	[Serializable]
	public sealed class CastIntent
	{
		public IntentOrigin Origin;              // Root / FollowUp
		public int CastId;                       // 루트 기준 ID(로그/RNG)
		public int ChainDepth;                   // 체인 깊이(루프 가드)
		public string SourceHook;                // 생성 훅(디버그)
		public string TemplateId;                // 템플릿 식별(디버그)

		public ISkillMechanic Mechanism;         // 실행 메커니즘
		public ISkillParam Param;                // 실행 파라미터
		public Transform TargetOverride;         // 필요 시 Runner의 EnsureAnchor 이전에 고정 타깃 지정

		public IntentTiming Timing;              // Immediate / Delayed
		public float DelaySeconds;               // Delayed 지연 시간(초)
		public bool RespectBusyCooldown;         // 바쁨/쿨다운 대기 여부

		public override string ToString() => $"[{Origin}] Cast={CastId} D={ChainDepth} Hook={SourceHook} Tpl={TemplateId} Mech={Mechanism?.GetType().Name} Timing={Timing}({DelaySeconds:F2})";

		// 팩토리: Root / FollowUp
		public static CastIntent Root(int castId, ISkillMechanic m, ISkillParam p, bool respectBusyCooldown, float delaySeconds = 0f, Transform targetOverride = null)
			=> new CastIntent { Origin = IntentOrigin.Root, CastId = castId, ChainDepth = 0, SourceHook = "<ROOT>", TemplateId = "root", Mechanism = m, Param = p, TargetOverride = targetOverride, Timing = delaySeconds > 0 ? IntentTiming.Delayed : IntentTiming.Immediate, DelaySeconds = Mathf.Max(0f, delaySeconds), RespectBusyCooldown = respectBusyCooldown };

		public static CastIntent FollowUp(int parentCastId, int depth, string sourceHook, string templateId, ISkillMechanic m, ISkillParam p, IntentTiming timing, float delaySeconds, bool respectBusyCooldown, Transform targetOverride = null)
			=> new CastIntent { Origin = IntentOrigin.FollowUp, CastId = parentCastId, ChainDepth = Mathf.Max(1, depth), SourceHook = sourceHook, TemplateId = templateId, Mechanism = m, Param = p, TargetOverride = targetOverride, Timing = timing, DelaySeconds = Mathf.Max(0f, delaySeconds), RespectBusyCooldown = respectBusyCooldown };
	}

	/* ✔ 무엇이 바뀌었나?
     * - (Before) TryCast/FollowUp→Schedule→StartCoroutine(CoCast)
     * - (After)  TryCast/FollowUp→Intent 생성→_intentQueue.Enqueue→FixedUpdate에서 소비→StartCoroutine(CoCast)
     *   즉, Intent가 CastOrder의 앞단에서 흐름을 일반화합니다.
     *
     * ✔ 어떻게 쓰나?
     * - 루트: SkillRunner.TryCast()가 내부적으로 EnqueueRootIntent(...)를 호출합니다.
     * - 팔로우업: IFollowUpProvider.BuildFollowUps(...)가 내놓는 CastOrder를
     *            BroadcastHook()에서 CastIntent.FollowUp(...)으로 감싸 큐에 넣습니다.
     * - 소비: FixedUpdate에서 Intent를 꺼내 Busy/CD/Delay 정책을 반영한 뒤 CoCast(order)로 실행합니다.
     *
     * ✔ 왜 이렇게 했나?
     * - 기존 CoCast(7단계)를 건드리지 않고, Intent 기능을 "비침투적으로" 도입하기 위해서입니다.
     * - Immediate FollowUp도 동틱 즉시 재귀 호출하지 않고, 한 틱의 꼬리 큐로만 처리할 수 있습니다.
     */

	#endregion // IntentSetting
}
