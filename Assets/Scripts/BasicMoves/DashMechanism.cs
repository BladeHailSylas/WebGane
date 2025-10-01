﻿using SkillInterfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EventBus;
using static GameEventMetaFactory;

[CreateAssetMenu(menuName = "Mechanics/Dash")]
public class DashMechanism : SkillMechanismBase<DashParams>, ITargetedMechanic
{
	public override IEnumerator Cast(Transform owner, Camera cam, DashParams p)
	{
		return Execute(owner, cam, p, null);
	}

	public IEnumerator Cast(Transform owner, Camera cam, ISkillParam param, Transform target)
		=> Execute(owner, cam, (DashParams)param, target);

	IEnumerator Execute(Transform owner, Camera cam, DashParams p, Transform explicitTarget)
	{
		if (!owner)
		{
			Debug.LogWarning("DashMechanism: Owner가 없어 실행을 중단합니다.");
			yield break;
		}

		var motor = owner.GetComponent<KinematicMotor2D>();
		if (!motor)
		{
			Debug.LogWarning("DashMechanism: KinematicMotor2D가 필요합니다.");
			yield break;
		}

		var solution = TargetingRuntimeUtil.Resolve(owner, cam, p, explicitTarget, createAnchor: false);
		Transform dashTarget = solution.IsSyntheticTarget ? null : solution.Target;
		Vector2 fallbackDir = solution.Direction.sqrMagnitude > 0f ? solution.Direction.normalized : (Vector2)owner.right;
		float desiredDist = dashTarget ? Vector2.Distance(owner.position, dashTarget.position) : solution.Distance;
		if (dashTarget && p.FallbackRange > 0f)
		{
			desiredDist = Mathf.Min(desiredDist, p.FallbackRange);
		}
		else if (!dashTarget && p.FallbackRange > 0f)
		{
			desiredDist = Mathf.Max(p.FallbackRange, desiredDist);
		}
		desiredDist = Mathf.Max(0f, desiredDist);
		if (desiredDist <= 0f)
		{
			Debug.LogWarning("DashMechanism: 이동 거리가 0이라 대시를 생략합니다.");
			yield break;
		}

		MechanismRuntimeUtil.QueueFollowUps(p, AbilityHook.OnCastStart, dashTarget, "Dash");

		if (p.grantIFrame && p.iFrameDuration > 0f)
		{
			// Publish(new EffectApplyReq(Create(owner,"combat"), owner, new IFrameEffect(), p.iFrameDuration));
		}

		Vector2 dir0 = fallbackDir.sqrMagnitude > 1e-4f ? fallbackDir : (Vector2)owner.right;
		float remaining = desiredDist;

		var basePolicy = motor.CurrentPolicy;
		var dashPolicy = basePolicy;
		dashPolicy.wallsMask = p.WallsMask;
		dashPolicy.enemyMask = p.enemyMask;
		dashPolicy.enemyAsBlocker = !p.CanPenetrate;
		dashPolicy.radius = p.radius;
		dashPolicy.skin = Mathf.Max(0.01f, p.skin);
		dashPolicy.allowWallSlide = true;

		using (motor.With(dashPolicy))
		{
			var hitIds = new HashSet<int>();
			float elapsed = 0f;
			float total = Mathf.Max(0.01f, p.duration);

			while (remaining > 0f)
			{
				using (motor.With(dashPolicy))
				{
					// 센서 기반 확장을 위한 placeholder입니다.
				}

				float tNorm = Mathf.Clamp01(elapsed / total);
				float nominalSpeed = (desiredDist / total) * p.speedCurve.Evaluate(tNorm);
				float stepDist = Mathf.Min(remaining, nominalSpeed * Time.deltaTime);

				Vector2 pos = owner.position;
				Vector2 aim = dashTarget ? (Vector2)dashTarget.position - pos : fallbackDir;
				Vector2 dir = aim.sqrMagnitude > 1e-4f ? aim.normalized : dir0;

				motor.Depenetration();
				var res = motor.SweepMove(dir * stepDist);
				motor.Depenetration();
				remaining -= res.actualDelta.magnitude;

				if (p.dealDamage && p.enemyMask.value != 0)
				{
					var hits = Physics2D.OverlapCircleAll(owner.position, p.radius, p.enemyMask);
					foreach (var c in hits)
					{
						int id = c.GetInstanceID();
						if (hitIds.Contains(id)) continue;
						if (c.TryGetComponent(out ActInterfaces.IVulnerable v))
						{
							v.TakeDamage(p.damage, p.apRatio);
							if (c.attachedRigidbody && p.knockback != 0f)
							{
								var kdir = ((Vector2)c.transform.position - (Vector2)owner.position).normalized;
								c.attachedRigidbody.AddForce(kdir * p.knockback, ForceMode2D.Impulse);
							}
							hitIds.Add(id);
							Publish(new DamageDealt(Create(owner, "combat"), owner, c.transform,
									p.damage, p.damage, 0f, EDamageType.Normal, owner.position, dir));
						}
					}
				}

				if (res.hitWall) break;

				if (dashTarget && dashTarget.TryGetComponent<Collider2D>(out _))
				{
					float arrive = p.radius + p.skin;
					if (((Vector2)dashTarget.position - (Vector2)owner.position).sqrMagnitude <= arrive * arrive)
						break;
				}

				elapsed += Time.deltaTime;
				if (elapsed >= total) break;

				yield return null;
			}

			motor.Depenetration();
		}

		MechanismRuntimeUtil.QueueFollowUps(p, AbilityHook.OnCastEnd, dashTarget, "Dash");
	}
}
