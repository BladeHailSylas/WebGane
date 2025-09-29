// ------------------------------------------------------------
// 0) 공용 정의: 버튼/입력/고정좌표/상태
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
namespace System.Runtime.CompilerServices { public static class IsExternalInit { } } // C# 9 record 용

[Flags]
public enum Buttons : ushort { None = 0, Light = 1, Heavy = 2, Dash = 4, Guard = 8 }

/// <summary> 프레임-결정적 입력. 실수 대신 정수 스냅 사용 </summary>
public struct InputBits
{
	public sbyte x;        // -100..100
	public sbyte y;        // -100..100
	public Buttons btn;    // 버튼 비트
}

/// <summary> 고정 소수점(16.16) 예시: 실제론 전용 Fixed32 타입 추천 </summary>
public struct Fixed2
{
	public int x, y; // 16.16 고정소수점 가정(=정수/65536f)
	public static Fixed2 FromFloat(float fx, float fy) => new() { x = (int)(fx * 65536f), y = (int)(fy * 65536f) };
	public static float ToFloatX(Fixed2 v) => v.x / 65536f;
	public static float ToFloatY(Fixed2 v) => v.y / 65536f;
}

/// <summary> 캐릭터 상태. 코어에서만 갱신 </summary>
public struct CharacterState
{
	public Fixed2 pos;
	public Fixed2 vel;

	public int facing;        // -1 or +1
	public int stunFrames;    // >0 이면 커맨드 불가(이동 제한 정책은 게임 규칙에 따름)
	public int currentMove;   // -1=Idle, 그 외=moveId
	public int moveTimer;     // 현재 동작의 진행 프레임(0..)
	public ushort flags;         // 무적/점프/가드 등 (비트 플래그)
	public int cancelLock;    // 캔슬 불가 잔여 프레임(예: 히트스톱/경직 등)
	public int lastHitFrame;  // 최근 피격 프레임(타이브레이커 등에 사용 가능)
							// 필요 시 게이지/체력/쿨타임 등 추가
}

/// <summary> 전역(세션 스코프) 게임 상태. 틱마다 하나만 존재 </summary>
public struct GameState
{
	public CharacterState P1, P2;
	public int Frame; // 현재 프레임 인덱스(0..)
}

// ------------------------------------------------------------
// 1) 규칙 데이터: 기술표/캔슬/히트정보
// ------------------------------------------------------------

public struct MoveDef
{
	public int moveId;
	public int startup;    // 시작 프레임 수
	public int active;     // 타격 가능 프레임 수
	public int recovery;   // 후딜 프레임 수
	public int hitstun;    // 피격자 스턴 프레임
	public int blockstun;  // 가드시 경직
	public int cancelFrom; // 캔슬 가능 시작 프레임(상대 히트 시)
	public int cancelTo;   // 캔슬 가능 종료 프레임
	public int nextOnFollowUp; // FollowUp 성공 시 연결될 moveId(없으면 -1)
	public int meterCost;  // 게이지 소모(없으면 0)
	public int priority;   // 동프레임 동시 히트 우선순위(낮을수록 먼저)
						   // 히트박스/넉백 등은 실제론 별도 테이블/프로파일로 분리 권장
}

/// <summary> 코어가 소비하는 규칙표 인터페이스(데이터 드리븐) </summary>
public interface IMoveTable
{
	MoveDef ResolveBySlot(int actorId, SkillSlot slot);
	MoveDef GetMove(int moveId);
	bool CanCancelTo(int curMoveId, int nextMoveId, bool onHit, int curMoveTimer);
}

/// <summary> 간단 샘플 구현(임시). 실제론 SO/JSON 로드 </summary>
public class SimpleMoveTable : IMoveTable
{
	readonly Dictionary<int, MoveDef> _defs = new();

	public SimpleMoveTable()
	{
		// 예시: 가벼운 약공격
		_defs[1] = new MoveDef
		{
			moveId = 1,
			startup = 5,
			active = 3,
			recovery = 12,
			hitstun = 16,
			blockstun = 8,
			cancelFrom = 6,
			cancelTo = 20,
			nextOnFollowUp = 2,
			meterCost = 0,
			priority = 1
		};
		// 예시: 연계 기술
		_defs[2] = new MoveDef
		{
			moveId = 2,
			startup = 4,
			active = 2,
			recovery = 16,
			hitstun = 18,
			blockstun = 10,
			cancelFrom = 5,
			cancelTo = 18,
			nextOnFollowUp = -1,
			meterCost = 0,
			priority = 0
		};
	}

	public MoveDef ResolveBySlot(int actorId, SkillSlot slot)
	{
		// 간단 매핑: 실제 게임은 캐릭터/스탠스/게이지 등에 따라 분기
		return slot switch
		{
			SkillSlot.Attack => _defs[1],
			SkillSlot.Skill1 => _defs[2],
			_ => _defs[1],
		};
	}

	public MoveDef GetMove(int moveId) => _defs[moveId];

	public bool CanCancelTo(int curMoveId, int nextMoveId, bool onHit, int curMoveTimer)
	{
		if (curMoveId < 0) return true; // Idle → 어떤 기술이든 OK
		var cur = _defs[curMoveId];
		// “히트 시에만 캔슬 가능” 같은 규칙도 onHit로 분기 가능
		if (!onHit) return false;
		return cur.cancelFrom <= curMoveTimer && curMoveTimer <= cur.cancelTo;
	}
}

public enum SkillSlot { Attack, Skill1, Skill2, Ultimate }

// ------------------------------------------------------------
// 2) 이벤트/포트: 코어⇄컨트롤러/런너 연결면
// ------------------------------------------------------------

public interface ICoreEvent { int Frame { get; } }

public record EvtMoveStart(int Frame, int ActorId, int MoveId) : ICoreEvent;
public record EvtMovePhase(int Frame, int ActorId, int MoveId, string Phase) : ICoreEvent; // "Startup/Active/Recovery"
public record EvtHitActive(int Frame, int ActorId, int MoveId, int HitboxId, bool On) : ICoreEvent;
public record EvtHitLanded(int Frame, int AttackerId, int DefenderId, int MoveId) : ICoreEvent;
public record EvtStunApplied(int Frame, int TargetId, int Frames) : ICoreEvent;
public record EvtFollowUpOpen(int Frame, int ActorId, int MoveId, int FromFrame, int ToFrame) : ICoreEvent;

/// <summary> 코어 이벤트를 수신하는 뷰 계층(=SkillRunner/연출) </summary>
public interface ICoreEventSink
{
	void Publish(ICoreEvent e);
}

/// <summary> 컨트롤러가 코어에 “의도”를 넣는 포트(온라인이면 applyFrame에 미래 프레임 사용) </summary>
public interface IInputPort
{
	void EnqueueAttackIntent(int applyFrame, int actorId, SkillSlot slot);
	// Dash/Guard 등도 같은 패턴으로 확장
}

// ------------------------------------------------------------
// 3) 코어 본체: 단 하나의 진실. 상태·판정·이벤트 생성 담당
// ------------------------------------------------------------

public sealed class SimulationCore : IInputPort
{
	public const int TICK_HZ = 60;
	public readonly IMoveTable MoveTable;
	public readonly ICoreEventSink Events;

	// 입력 의도 큐(프레임별). 온라인/롤백 시 히스토리와 같이 관리됩니다.
	private readonly Dictionary<int, List<(int actorId, SkillSlot slot)>> _attackIntents = new();

	public SimulationCore(IMoveTable moveTable, ICoreEventSink sink)
	{
		MoveTable = moveTable;
		Events = sink;
	}

	// ===== Controller → Core : “의도”만 적재 =====
	public void EnqueueAttackIntent(int applyFrame, int actorId, SkillSlot slot)
	{
		if (!_attackIntents.TryGetValue(applyFrame, out var list))
		{
			list = new List<(int, SkillSlot)>(2);
			_attackIntents[applyFrame] = list;
		}
		list.Add((actorId, slot));
	}

	// ===== 틱 진행: 입력 소비 → 규칙 → 상태 갱신 → 이벤트 =====
	public GameState Step(GameState s, InputBits p1, InputBits p2)
	{
		s.Frame++;

		// 1) 스턴/경직 카운트다운
		DecayCommon(ref s.P1);
		DecayCommon(ref s.P2);

		// 2) 이동(키네마틱 충돌/디펜은 여기서 — 이미 구현하신 로직 연결)
		ApplyLocomotion(ref s.P1, p1);
		ApplyLocomotion(ref s.P2, p2);

		// 3) 공격 의도 소화(해당 프레임에 쌓인 것만)
		if (_attackIntents.TryGetValue(s.Frame, out var intents))
		{
			// 동프레임 충돌 대비: 우선순위/타이브레이커를 한 곳에서 처리
			intents.Sort((a, b) => a.actorId.CompareTo(b.actorId)); // 간단 예: P1→P2 순
			foreach (var (actor, slot) in intents)
				TryStartMove(actor, slot, ref s);
		}

		// 4) 현재 진행 중인 기술의 타임라인 진행/히트박스 On/Off → 뷰 이벤트
		ProgressMoves(ref s);

		// 5) 히트 판정(결정적 순서: 항상 P1의 히트→P2, 그 다음 P2→P1)
		ResolveHits(ref s);

		return s;
	}

	// --------------------------------------------------------
	// 내부: 이동/기술 시작/타임라인/히트/스턴
	// --------------------------------------------------------

	private static void DecayCommon(ref CharacterState f)
	{
		if (f.stunFrames > 0) f.stunFrames--;
		if (f.cancelLock > 0) f.cancelLock--;
		// 히트스톱/버프/디버프 등도 이곳에서 감소
	}

	private static void ApplyLocomotion(ref CharacterState f, InputBits inp)
	{
		// 스턴 중 이동 금지(정책은 게임에 맞게 조정)
		var ax = (f.stunFrames > 0) ? (sbyte)0 : inp.x;
		// 간단: 속도 = 입력 * 고정속도(정수)
		const int SPEED = 1800; // 1800/65536 ≈ 0.0275 unit/frame
		f.vel.x = ax * SPEED;
		f.pos.x += f.vel.x;

		// TODO: 키네마틱 월드 충돌/디펜 호출(결정적 순서/정수 기반)
	}

	private void TryStartMove(int actorId, SkillSlot slot, ref GameState s)
	{
		ref var self = ref (actorId == 0 ? ref s.P1 : ref s.P2);
		ref var other = ref (actorId == 0 ? ref s.P2 : ref s.P1);

		// 스턴/락 중이면 발동 불가
		if (self.stunFrames > 0 || self.cancelLock > 0) return;

		// 현재 동작과 캔슬 규칙 검사
		var next = MoveTable.ResolveBySlot(actorId, slot);

		bool onHit = (other.lastHitFrame == s.Frame - 1); // 아주 단순한 예시(직전 프레임 히트)
		bool canCancel = MoveTable.CanCancelTo(self.currentMove, next.moveId, onHit, self.moveTimer);

		if (self.currentMove >= 0 && !canCancel) return;

		// 발동 승인
		self.currentMove = next.moveId;
		self.moveTimer = 0;
		Events.Publish(new EvtMoveStart(s.Frame, actorId, next.moveId));
		Events.Publish(new EvtMovePhase(s.Frame, actorId, next.moveId, "Startup"));

		// FollowUp 윈도우 오픈(데이터 기반 권장)
		if (next.nextOnFollowUp > 0)
		{
			int from = next.cancelFrom, to = next.cancelTo;
			Events.Publish(new EvtFollowUpOpen(s.Frame, actorId, next.moveId, from, to));
		}
	}

	private void ProgressMoves(ref GameState s)
	{
		ProgressMove(0, ref s.P1, ref s);
		ProgressMove(1, ref s.P2, ref s);
	}

	private void ProgressMove(int actorId, ref CharacterState f, ref GameState s)
	{
		if (f.currentMove < 0) return;

		var def = MoveTable.GetMove(f.currentMove);
		f.moveTimer++;

		if (f.moveTimer == def.startup)
		{
			Events.Publish(new EvtMovePhase(s.Frame, actorId, def.moveId, "Active"));
			// 필요 시 히트박스 ON 이벤트
			Events.Publish(new EvtHitActive(s.Frame, actorId, def.moveId, 0, On: true));
		}

		if (f.moveTimer == def.startup + def.active)
		{
			Events.Publish(new EvtMovePhase(s.Frame, actorId, def.moveId, "Recovery"));
			Events.Publish(new EvtHitActive(s.Frame, actorId, def.moveId, 0, On: false));
		}

		if (f.moveTimer >= def.startup + def.active + def.recovery)
		{
			// 동작 종료
			f.currentMove = -1;
			f.moveTimer = 0;
		}
	}

	private void ResolveHits(ref GameState s)
	{
		// ★ 실제론 히트박스 vs 허트박스, 무적/가드/우선순위 등 정교한 판정 필요
		TryHit(attackerId: 0, ref s.P1, ref s.P2, ref s);
		TryHit(attackerId: 1, ref s.P2, ref s.P1, ref s);
	}

	private void TryHit(int attackerId, ref CharacterState atk, ref CharacterState def, ref GameState s)
	{
		if (atk.currentMove < 0) return;
		var defMove = MoveTable.GetMove(atk.currentMove);

		// 예시: Active 중이면 “히트한 걸로 간주” (여기만 바꿔도 즉시 실험 가능)
		bool isActive = atk.moveTimer >= defMove.startup && atk.moveTimer < defMove.startup + defMove.active;
		if (!isActive) return;

		// 타이브레이커/우선순위 등은 여기서
		ApplyHit(attackerId, ref atk, ref def, defMove.hitstun, ref s);
	}

	private void ApplyHit(int attackerId, ref CharacterState atk, ref CharacterState def, int stun, ref GameState s)
	{
		// 스턴/경직 부여 (최대값 우선)
		def.stunFrames = Math.Max(def.stunFrames, stun);
		def.lastHitFrame = s.Frame;
		Events.Publish(new EvtHitLanded(s.Frame, attackerId, attackerId == 0 ? 1 : 0, atk.currentMove));
		Events.Publish(new EvtStunApplied(s.Frame, attackerId == 0 ? 1 : 0, stun));

		// 히트 시 일정 프레임 캔슬 가능 → cancelLock 해제 등 규칙을 MoveTable.CanCancelTo에서 처리
	}

	// --------------------------------------------------------
	// 4) 체크섬(디싱크 검출용). 롤백/리플레이에서 사용
	// --------------------------------------------------------
	public static uint Checksum(in GameState s)
	{
		unchecked
		{
			uint h = 2166136261;
			void mix(int v) { h = (h ^ (uint)v) * 16777619; }
			mix(s.Frame);
			mix(s.P1.pos.x); mix(s.P1.pos.y); mix(s.P1.stunFrames); mix(s.P1.currentMove); mix(s.P1.moveTimer);
			mix(s.P2.pos.x); mix(s.P2.pos.y); mix(s.P2.stunFrames); mix(s.P2.currentMove); mix(s.P2.moveTimer);
			return h;
		}
	}
}