using System;
using System.Text;
using UnityEngine;

/// <summary>
/// Player보다 약간 큰 Trigger Collider 2D에 부착하는 "감지 전용" 센서.
/// - 벽/적에 "임박(겹치기 전)" 상태를 조기에 감지하고, 평균 법선/최소 거리/겹침 여부를 계산합니다.
/// - 절대 위치를 바꾸지 않습니다(이동 결정은 모터의 스윕에서만).
/// - Debug.Log로 상태 변화를 요약해 출력(스팸 방지용 쿨다운 포함).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class PlayerSensor2D : MonoBehaviour
{
    [Header("Masks")]
    [SerializeField] LayerMask wallsMask;
    [SerializeField] LayerMask enemyMask;

    [Header("Sampling")]
    [Tooltip("센서 반경 여유(본체 반경보다 얼마나 더 크게 둘지). 참고값: 0.03~0.08m")]
    [SerializeField] float skin = 0.05f;

    [Tooltip("프레임당 검사 최대 개수(NonAlloc 버퍼)")]
    [SerializeField] int maxHits = 8;

    [Tooltip("Log 출력 쿨다운(초). 0이면 매 프레임 로그")]
    [SerializeField] float logCooldown = 0.25f;

    Collider2D sensor;                       // 센서(Trigger)
    readonly Collider2D[] hits = null;       // NonAlloc 버퍼
    ContactFilter2D filterWalls, filterEnemy;

    float nextLogAt;

    // === 센서가 노출하는 읽기 전용 상태 ===
    public bool NearWall { get; private set; }
    public bool NearEnemy { get; private set; }
    public bool Intruding { get; private set; }           // (distance < 0) → 이미 겹침
    public Vector2 WallAvgNormal { get; private set; }    // 임박한 벽들의 평균 법선(정규화)
    public float WallMinDistance { get; private set; }    // 센서 경계로부터 가장 가까운 벽까지의 거리(미터)
    public Transform NearestHit { get; private set; }     // 가장 가까운 충돌체(디버깅/효과용)
    public Vector2 MTVDir { get; private set; }           // 겹침인 경우, 밖으로 나가는 최소 이탈 방향

    // NonAlloc 버퍼 생성자
    public PlayerSensor2D()
    {
        hits = new Collider2D[Mathf.Max(4, 8)];
    }

    void Awake()
    {
        sensor = GetComponent<Collider2D>();
        sensor.isTrigger = true;

        // 벽/적 별도의 ContactFilter2D 구성
        filterWalls = new ContactFilter2D { useLayerMask = true, layerMask = wallsMask, useTriggers = false };
        filterEnemy = new ContactFilter2D { useLayerMask = true, layerMask = enemyMask, useTriggers = true }; // 적의 trigger hurtbox도 감지하려면 true
    }

    void Update()
    {
        Sample();                // 매 프레임 샘플링
        DebugSampleIfNeeded();   // 상태 요약 로그
    }

    /// <summary>센서 영역에서 벽/적 후보를 모아 Distance 샘플링으로 상태를 집계합니다.</summary>
    void Sample()
    {
        // 초기화
        NearWall = NearEnemy = Intruding = false;
        WallAvgNormal = Vector2.zero;
        WallMinDistance = float.PositiveInfinity;
        NearestHit = null;
        MTVDir = Vector2.zero;

        int countWalls = sensor.Overlap(filterWalls, hits);
        int countEnemy = sensor.Overlap(filterEnemy, hits);

        // --- 벽 샘플링 ---
        int nNormals = 0;
        float best = float.PositiveInfinity;
        Collider2D bestCol = null;

        for (int i = 0; i < countWalls; i++)
        {
            var other = hits[i];
            if (!other) continue;

            // ColliderDistance2D: 센서와 상대 간의 최소 이탈 정보
            var dist = Physics2D.Distance(sensor, other);
            // dist.distance > 0 : 떨어져 있음(양수=간격), ==0 : 접촉, <0 : 겹침(음수=침투량)

            if (dist.distance <= skin + 1e-4f) NearWall = true; // 임박 임계
            if (dist.distance < 0f) Intruding = true;           // 이미 겹침

            // 평균 법선(멀리 떨어진 건 제외)
            if (dist.distance <= skin * 2f)
            {
                WallAvgNormal += dist.normal; // dist.normal: 센서에서 상대로의 바깥 법선(문서 기준)
                nNormals++;
            }

            // 가장 가까운 벽 기록
            if (dist.distance < best)
            {
                best = dist.distance;
                bestCol = other;
                // 겹침이면 MTV는 '밖으로 나가는 방향' = -dist.normal
                if (dist.distance < 0f) MTVDir = -dist.normal;
            }
        }

        if (nNormals > 0) WallAvgNormal = WallAvgNormal.normalized;
        WallMinDistance = (best < float.PositiveInfinity) ? Mathf.Max(0f, best) : float.PositiveInfinity;
        NearestHit = bestCol ? bestCol.transform : null;

        // --- 적 샘플링(근접 여부만) ---
        for (int i = 0; i < countEnemy && !NearEnemy; i++)
        {
            var other = hits[i];
            if (!other) continue;
            var dist = Physics2D.Distance(sensor, other);
            // 적은 '근접 신호'만 사용(차단은 정책에서 제어)
            if (dist.distance <= skin + 1e-4f) NearEnemy = true;
            if (dist.distance < 0f) Intruding = true; // 적과 겹친 경우도 플래그
        }
    }

    void DebugSampleIfNeeded()
    {
        if (Time.time < nextLogAt) return;

        var sb = new StringBuilder();
        sb.Append($"[Sensor] nearWall={NearWall}, nearEnemy={NearEnemy}, intruding={Intruding}");
        sb.Append($", wallMinDist={(float.IsInfinity(WallMinDistance) ? -1f : WallMinDistance):F3}");
        if (WallAvgNormal != Vector2.zero) sb.Append($", wallAvgN={WallAvgNormal}");
        if (MTVDir != Vector2.zero) sb.Append($", mtvDir={MTVDir}");
        if (NearestHit) sb.Append($", nearest={NearestHit.name}");
        //Debug.Log(sb.ToString(), this);

        nextLogAt = Time.time + Mathf.Max(0f, logCooldown);
    }

    // 디버깅 시 시각화(에디터 Scene 뷰)
    void OnDrawGizmosSelected()
    {
        if (!enabled) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        // 평균 벽 법선
        if (WallAvgNormal != Vector2.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)WallAvgNormal * 0.5f);
        }
        // MTV
        if (MTVDir != Vector2.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)MTVDir * 0.5f);
        }
    }

    // === 외부에서 읽기 쉽게 상태 묶음을 제공(선택) ===
    public SensorState GetState() => new()
    {
        nearWall = NearWall,
        nearEnemy = NearEnemy,
        intruding = Intruding,
        wallAvgNormal = WallAvgNormal,
        wallMinDistance = WallMinDistance,
        mtvDir = MTVDir,
        nearest = NearestHit
    };

    public struct SensorState
    {
        public bool nearWall, nearEnemy, intruding;
        public Vector2 wallAvgNormal, mtvDir;
        public float wallMinDistance;
        public Transform nearest;
    }
}
