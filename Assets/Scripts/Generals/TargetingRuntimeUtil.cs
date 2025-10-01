using ActInterfaces;
using SkillInterfaces;
using UnityEngine;

/// <summary>
///     타깃팅과 관련된 런타임 계산을 표준화합니다. SkillRunner/IntentOrchestrator 체계를 고려해 재사용 가능한 유틸리티를 제공합니다.
/// </summary>
public static class TargetingRuntimeUtil
{
    /// <summary>
    ///     Synthetic Anchor에 부여하는 이름입니다. ProjectileMovement 등 기존 코드와 호환하기 위해 상수로 정의합니다.
    /// </summary>
    public const string AnchorName = "Anchor";

    const float MinDirectionSqr = 1e-4f;

    /// <summary>
    ///     현재 캐스트 환경에서 사용할 타깃 정보를 계산합니다.
    /// </summary>
    /// <param name="owner">기술을 시전하는 주체입니다.</param>
    /// <param name="cam">시야 계산에 활용할 카메라입니다.</param>
    /// <param name="data">타깃팅 모드를 정의한 파라미터입니다.</param>
    /// <param name="explicitTarget">Intent에서 이미 결정된 대상입니다.</param>
    /// <param name="createAnchor">명시적 대상이 없을 때 Anchor GameObject를 생성할지 여부입니다.</param>
    public static TargetingResult Resolve(Transform owner, Camera cam, ITargetingData data, Transform explicitTarget, bool createAnchor = false)
    {
        if (owner == null)
        {
            return new TargetingResult(explicitTarget, Vector2.right, 0f, Vector2.zero, null, false);
        }

        Vector2 origin = owner.position;

        if (explicitTarget != null)
        {
            var offset = (Vector2)explicitTarget.position - origin;
            if (offset.sqrMagnitude <= MinDirectionSqr)
            {
                offset = owner.right;
            }
            float distance = offset.magnitude;
            var dir = offset.sqrMagnitude > MinDirectionSqr ? offset.normalized : (Vector2)owner.right;
            return new TargetingResult(explicitTarget, dir, distance, (Vector2)explicitTarget.position, null, false);
        }

        Vector2 desiredPoint = origin;
        Vector2 fallbackDir = owner.right;
        float fallbackDistance = data != null ? Mathf.Max(0f, data.FallbackRange) : 0f;

        if (data != null)
        {
            switch (data.Mode)
            {
                case TargetMode.TowardsCursor when cam != null:
                    var mouse = cam.ScreenToWorldPoint(Input.mousePosition);
                    mouse.z = owner.position.z;
                    desiredPoint = mouse;
                    fallbackDir = desiredPoint - origin;
                    fallbackDistance = fallbackDir.magnitude;
                    break;
                case TargetMode.TowardsOffset:
                    desiredPoint = owner.TransformPoint(data.LocalOffset);
                    fallbackDir = desiredPoint - origin;
                    fallbackDistance = fallbackDir.magnitude;
                    break;
                case TargetMode.TowardsMovement:
                    if (owner.TryGetComponent<IMovable>(out var mover) && mover.LastMoveDir.sqrMagnitude > MinDirectionSqr)
                    {
                        fallbackDir = mover.LastMoveDir;
                    }
                    else if (owner.TryGetComponent<Rigidbody2D>(out var rb) && rb.linearVelocity.sqrMagnitude > MinDirectionSqr)
                    {
                        fallbackDir = rb.linearVelocity;
                    }
                    desiredPoint = origin + fallbackDir.normalized * Mathf.Max(data.FallbackRange, fallbackDistance);
                    fallbackDistance = data.FallbackRange;
                    break;
                case TargetMode.TowardsEnemy:
                    /** 향후 Targeter 시스템과 연동하여 실제 적 탐색을 구현할 수 있도록 이 분기에서 Hook를 남겨둡니다. */
                    desiredPoint = origin + fallbackDir.normalized * Mathf.Max(data.FallbackRange, fallbackDistance);
                    fallbackDistance = Mathf.Max(data.FallbackRange, fallbackDistance);
                    break;
                default:
                    desiredPoint = origin + fallbackDir.normalized * Mathf.Max(data.FallbackRange, fallbackDistance);
                    fallbackDistance = Mathf.Max(data.FallbackRange, fallbackDistance);
                    break;
            }
        }
        else
        {
            desiredPoint = origin + fallbackDir.normalized * Mathf.Max(fallbackDistance, 0f);
        }

        if (fallbackDir.sqrMagnitude <= MinDirectionSqr)
        {
            fallbackDir = owner.right;
        }
        if (fallbackDir.sqrMagnitude <= MinDirectionSqr)
        {
            fallbackDir = Vector2.right;
        }

        fallbackDir = fallbackDir.normalized;
        if (fallbackDistance <= 0f)
        {
            fallbackDistance = (desiredPoint - origin).magnitude;
            if (fallbackDistance <= 0f)
            {
                fallbackDistance = 0.01f;
            }
        }

        Transform anchor = null;
        bool synthetic = false;
        if (createAnchor)
        {
            var anchorObj = new GameObject(AnchorName);
            anchorObj.transform.position = desiredPoint;
            anchorObj.hideFlags = HideFlags.DontSave;
            anchor = anchorObj.transform;
            synthetic = true;
        }

        return new TargetingResult(anchor, fallbackDir, fallbackDistance, desiredPoint, anchor, synthetic);
    }

    /// <summary>
    ///     Resolve 결과를 담는 구조체입니다. Anchor 관리 기능을 함께 제공합니다.
    /// </summary>
    public readonly struct TargetingResult
    {
        public Transform Target { get; }
        public Vector2 Direction { get; }
        public float Distance { get; }
        public Vector2 TargetPoint { get; }
        public bool IsSyntheticTarget { get; }

        readonly Transform _anchor;

        internal TargetingResult(Transform target, Vector2 direction, float distance, Vector2 targetPoint, Transform anchor, bool synthetic)
        {
            Target = target;
            Direction = direction;
            Distance = distance;
            TargetPoint = targetPoint;
            _anchor = anchor;
            IsSyntheticTarget = synthetic;
        }

        /// <summary>
        ///     Anchor를 지정된 부모 아래로 이동시켜 수명 관리를 쉽게 합니다.
        /// </summary>
        public void AdoptAnchor(Transform parent)
        {
            if (_anchor != null)
            {
                _anchor.SetParent(parent, true);
            }
        }

        /// <summary>
        ///     Anchor가 더 이상 필요 없을 때 명시적으로 제거합니다.
        /// </summary>
        public void DisposeAnchor()
        {
            if (_anchor != null)
            {
                Object.Destroy(_anchor.gameObject);
            }
        }
    }
}
