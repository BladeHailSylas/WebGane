using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SkillInterfaces;
using ActInterfaces;
using Unity.VisualScripting;

namespace Intents
{
	/// <summary>
	///     전역 Orchestrator. FixedUpdate 단일 소비자로 Intent 파이프라인을 처리합니다.
	/// </summary>
	public sealed class IntentOrchestrator : MonoBehaviour, IIntentSink
	{
		public static IntentOrchestrator Instance { get; private set; }

		[Header("Determinism")]
		[SerializeField] int matchSeed = 0x13572468;
		[SerializeField] int maxChainDepth = 8;
		[SerializeField] int perTickBudget = 64;

		[Header("Diagnostics")]
		[SerializeField] bool verboseLog;

		private readonly Queue<CastIntent> _globalQueue = new();
		private readonly Queue<CastIntent> _immediateQueue = new();
		private readonly List<CastIntent> _tickBuffer = new();
		private readonly List<CastIntent> _followUpBuffer = new();
		private readonly HashSet<string> _guardSet = new();
		private readonly HashSet<string> _tickDedup = new();
		private readonly Dictionary<int, int> _busyUntilTick = new();
		private readonly Dictionary<int, int> _cooldownUntilTick = new();

		private int _tick;
		private int _bufferIndex;
		private bool _processingImmediate;

		private readonly StringBuilder _tickLog = new(256);

		public void TieBreaker(params IVulnerable[] damaged)
		{
			if (damaged == null || damaged.Length == 0) return;
			foreach(var d in damaged)
			{
				if (d == null) continue;
				Debug.Log($"TieBreaker hitted");
				d.Die();
			}
		}
		void Awake()
		{
			Instance = this;
			Debug.Log($"Intent Instance(Hash {GetHashCode()})이 생성되었습니다");
			
			/*if (Instance != null && Instance != this)
			{
				Debug.LogError("중복 IntentOrchestrator 감지 – Determinism이 깨질 수 있습니다.");
			}*/
		}

		public void Enqueue(CastIntent intent)
		{
			if (intent == null) throw new ArgumentNullException(nameof(intent));
			if (intent.ChainDepth > maxChainDepth)
			{
				Debug.LogWarning($"Intent {intent} 가 MaxChainDepth({maxChainDepth}) 초과로 폐기되었습니다.");
				return;
			}
			intent.ScheduledTick = Math.Max(intent.ScheduledTick, _tick);
			_globalQueue.Enqueue(intent);
		}

		public void AddIntent(CastIntent intent)
		{
			// FollowUp만 허용. Immediate 재귀를 막기 위해 Queue에 적재만 수행합니다.
			if (intent == null) return;
			if (intent.ChainDepth > maxChainDepth)
			{
				Debug.LogWarning($"FollowUp {intent} MaxChainDepth 초과 – 무시되었습니다.");
				return;
			}
			intent.ScheduledTick = Math.Max(intent.ScheduledTick, _tick);
			if (intent.Timing == IntentTiming.Immediate)
			{
				_immediateQueue.Enqueue(intent);
			}
			else
			{
				_globalQueue.Enqueue(intent);
			}
		}

		public bool IsActorBusy(int actorId)
		{
			return _busyUntilTick.TryGetValue(actorId, out var until) && until > _tick;
		}

		public bool IsActorOnCooldown(int actorId)
		{
			return _cooldownUntilTick.TryGetValue(actorId, out var until) && until > _tick;
		}

		void FixedUpdate()
		{
			_tick++;
			PrepareTickBuffer();
			CastScope.Reset();
			_tickDedup.Clear();
			_tickLog.Length = 0;

			int dequeued = 0;
			int executed = 0;
			int blockedDepth = 0;
			int blockedDedup = 0;

			int budget = perTickBudget;
			while (budget-- > 0 && TryFetchIntent(out var intent))
			{
				dequeued++;

				if (intent.ChainDepth > maxChainDepth)
				{
					blockedDepth++;
					continue;
				}

				if (!string.IsNullOrEmpty(intent.DedupKey))
				{
					if (!_tickDedup.Add(intent.DedupKey))
					{
						blockedDedup++;
						continue;
					}
				}

				if (ProcessIntent(intent))
				{
					executed++;
				}
			}

			for (int i = _bufferIndex; i < _tickBuffer.Count; i++)
			{
				var carry = _tickBuffer[i];
				carry.ScheduledTick = _tick + 1;
				_globalQueue.Enqueue(carry);
			}

			// Immediate 큐 잔여분 carry-over
			while (_immediateQueue.Count > 0)
			{
				var carry = _immediateQueue.Dequeue();
				carry.ScheduledTick = _tick + 1; // 재귀 금지 → 다음 틱으로 이월
				_globalQueue.Enqueue(carry);
			}

			if (verboseLog)
			{
				Debug.LogFormat(
					"[IntentTick#{0}] dequeued={1} executed={2} blocked(depth={3}, dedup={4}) carry={5}\n{6}",
					_tick,
					dequeued,
					executed,
					blockedDepth,
					blockedDedup,
					_globalQueue.Count,
					_tickLog.ToString());
			}
		}

		private void PrepareTickBuffer()
		{
			_tickBuffer.Clear();
			_bufferIndex = 0;
			int count = _globalQueue.Count;
			for (int i = 0; i < count; i++)
			{
				var intent = _globalQueue.Dequeue();
				if (intent.ScheduledTick > _tick)
				{
					_globalQueue.Enqueue(intent);
					continue;
				}
				_tickBuffer.Add(intent);
			}
			_tickBuffer.Sort((a, b) => b.PriorityLevel.CompareTo(a.PriorityLevel));
		}

		private bool TryFetchIntent(out CastIntent intent)
		{
			if (_immediateQueue.Count > 0)
			{
				intent = _immediateQueue.Dequeue();
				_processingImmediate = true;
				return true;
			}

			_processingImmediate = false;
			if (_bufferIndex < _tickBuffer.Count)
			{
				intent = _tickBuffer[_bufferIndex++];
				return true;
			}
			intent = null;
			return false;
		}

		private bool ProcessIntent(CastIntent intent)
		{
			var actorBusy = IsActorBusy(intent.OriginActorId);
			var actorCd = IsActorOnCooldown(intent.OriginActorId);
			if (intent.RespectBusyCooldown && (actorBusy || actorCd))
			{
				// Busy 상태면 다음 틱으로 이월
				intent.ScheduledTick = _tick + 1;
				_globalQueue.Enqueue(intent);
				return false;
			}

			if (!HandleDelayOrPeriodic(intent))
			{
				return false;
			}

			var context = BuildContext(intent);
			if (!Validate(intent, context))
			{
				AppendIntentLog(intent, "ValidateFail", null, guardPassed: false);
				return false;
			}

			if (!BeginCost(intent, context))
			{
				AppendIntentLog(intent, "BeginCostFail", null, guardPassed: false);
				return false;
			}

			var target = ResolveTarget(intent, context);
			var execResult = Execute(intent, context, target);
			Apply(intent, context, execResult);
			ScheduleFollowUps(intent, context, execResult);
			Finalize(intent, context, execResult);

			AppendIntentLog(intent, "Finalize", target, guardPassed: true);
			return true;
		}

		private bool HandleDelayOrPeriodic(CastIntent intent)
		{
			if (intent.Timing == IntentTiming.Periodic)
			{
				if (intent.IntervalTicks <= 0)
				{
					Debug.LogWarning("Periodic Intent에 IntervalTicks가 0입니다.");
				}
				else
				{
					// Periodic은 실행 후 재스케줄됩니다.
					var next = intent.Clone();
					next.ScheduledTick = _tick + intent.IntervalTicks;
					_globalQueue.Enqueue(next);
				}
			}
			return true;
		}

		private CastContext BuildContext(CastIntent intent)
		{
			var rngSeed = matchSeed ^ _tick ^ intent.RootCastId;
			return new CastContext(intent, _tick, new System.Random(rngSeed));
		}

		private bool Validate(CastIntent intent, CastContext context)
		{
			if (intent.Mechanism == null)
			{
				Debug.LogWarning($"{intent} 메커니즘이 없습니다.");
				return false;
			}
			if (intent.Param == null)
			{
				Debug.LogWarning($"{intent} 파라미터가 없습니다.");
				return false;
			}
			if (!intent.Mechanism.ParamType.IsInstanceOfType(intent.Param))
			{
				Debug.LogError($"Intent {intent.RootCastId} ParamType mismatch");
				return false;
			}
			return true;
		}

		private bool BeginCost(CastIntent intent, CastContext context)
		{
			if (!string.IsNullOrEmpty(intent.GuardKey))
			{
				if (!_guardSet.Add(intent.GuardKey))
				{
					// Guard 실패 → Fail 처리
					return false;
				}
			}

			// Busy/CD 점유. 실제 Busy 기간 계산은 메커니즘별 정책 필요.
			_busyUntilTick[intent.OriginActorId] = _tick + 1;

			if (intent.Param is ICooldownParam hasCd)
			{
				int cdTicks = Mathf.CeilToInt(hasCd.Cooldown * 60f); // TODO: FrameRate 의존성 조정 필요
				_cooldownUntilTick[intent.OriginActorId] = _tick + cdTicks;
			}
			return true;
		}

		private Transform ResolveTarget(CastIntent intent, CastContext context)
		{
			switch (intent.TargetRequest.Policy)
			{
				case TargetPolicy.SameAsCast:
					return intent.TargetRequest.ExplicitActor;
				case TargetPolicy.UseHitTarget:
					// TODO: 히트 타겟을 저장할 구조 필요.
					break;
				case TargetPolicy.PickClosest:
					// TODO: 팀/반경 필터링 구현 필요.
					break;
			}
			return intent.TargetRequest.ExplicitActor;
		}

		private ExecutionResult Execute(CastIntent intent, CastContext context, Transform target)
		{
			_followUpBuffer.Clear();
			using (CastScope.Enter(this, intent, context, target))
			{
				try
				{
					IEnumerator routine;
					var owner = context.Owner != null ? context.Owner : transform;
					// 잠재적 문제: Owner null 시 Orchestrator의 Transform을 사용하므로, 멀티 액터 환경에서 오동작 가능.
					if (intent.Mechanism is ITargetedMechanic targeted && target != null)
					{
						routine = targeted.Cast(owner, context.Camera, intent.Param, target);
					}
					else
					{
						routine = intent.Mechanism.Cast(owner, context.Camera, intent.Param);
					}
					if (routine != null)
					{
						StartCoroutineSafe(routine);
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Intent 실행 중 예외: {ex}");
					return new ExecutionResult(false, Array.Empty<CastIntent>());
				}
			}
			return new ExecutionResult(true, _followUpBuffer.ToArray());
		}

		private void Apply(CastIntent intent, CastContext context, ExecutionResult result)
		{
			// Apply 단계는 실제 게임 로직에 맞게 확장 필요.
			// 현재는 Determinism을 위한 placeholder만 제공합니다.
		}

		private void ScheduleFollowUps(CastIntent intent, CastContext context, ExecutionResult result)
		{
			if (result.FollowUps == null) return;
			foreach (var follow in result.FollowUps)
			{
				if (follow.ChainDepth > maxChainDepth)
				{
					Debug.LogWarning($"FollowUp Depth 초과: {follow.ChainDepth} > {maxChainDepth}");
					continue;
				}
				follow.ScheduledTick = _processingImmediate ? _tick : intent.ScheduledTick;
				if (follow.Timing == IntentTiming.Immediate)
				{
					_immediateQueue.Enqueue(follow);
				}
				else
				{
					if (follow.Timing == IntentTiming.Delayed && follow.DelayTicks > 0)
					{
						follow.ScheduledTick = _tick + follow.DelayTicks;
					}
					_globalQueue.Enqueue(follow);
				}
			}
		}

		private void Finalize(CastIntent intent, CastContext context, ExecutionResult result)
		{
			if (!string.IsNullOrEmpty(intent.GuardKey))
			{
				_guardSet.Remove(intent.GuardKey);
			}
			_busyUntilTick.Remove(intent.OriginActorId);
		}

		private void StartCoroutineSafe(IEnumerator routine)
		{
			if (routine == null) return;
			if (!isActiveAndEnabled)
			{
				Debug.LogWarning("Orchestrator 비활성 상태에서 코루틴 실행 요청이 들어왔습니다.");
				return;
			}
			StartCoroutine(routine);
		}

		private void AppendIntentLog(CastIntent intent, string hook, Transform target, bool guardPassed)
		{
			_tickLog.AppendFormat(
				"CastId={0} Depth={1} Hook={2} Tpl={3} Timing={4} Priority={5} Target={6} Guard={7}\n",
				intent.RootCastId,
				intent.ChainDepth,
				hook,
				intent.TemplateId,
				intent.Timing,
				intent.PriorityLevel,
				target ? target.name : "<null>",
				guardPassed ? "OK" : "BLOCK");
		}

		void IIntentSink.AddIntent(CastIntent intent)
		{
			_followUpBuffer.Add(intent);
		}
	}

	/// <summary>
	///     CastContext는 파이프라인 중 생성되는 실행 환경입니다.
	/// </summary>
	public sealed class CastContext
	{
		public readonly CastIntent Intent;
		public readonly int Tick;
		public readonly System.Random Rng;
		public readonly Transform Owner;
		public readonly Camera Camera;

		public CastContext(CastIntent intent, int tick, System.Random rng)
		{
			Intent = intent;
			Tick = tick;
			Rng = rng;
			Owner = intent.OriginTransform;
			Camera = intent.Camera != null ? intent.Camera : Camera.main;
		}
	}
}
