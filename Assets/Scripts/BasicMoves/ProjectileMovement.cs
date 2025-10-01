using UnityEngine;
using ActInterfaces;
using System;
using System.Collections.Generic;

public class ProjectileMovement : MonoBehaviour, IExpirable
{
	MissileParams P;
	Transform owner, target;
	Vector2 dir;
	float speed, traveled, life;
	public float Lifespan => life;

	// ¡Ú ÀÌ¹Ì ¸ÂÃá ÄÝ¶óÀÌ´õ ÀçÅ¸°Ý ¹æÁö
	readonly HashSet<int> _hitIds = new();
	const float SKIN = 0.01f; // Ãæµ¹¸éÀ» »ìÂ¦ ³Ñ¾î°¡µµ·Ï

	public void Init(MissileParams p, Transform owner, Transform target)
	{
		P = p; this.owner = owner; this.target = target;
		Vector2 start = owner.position;
		Vector2 tgt = target ? (Vector2)target.position : start + Vector2.right;
		dir = (tgt - start).normalized;
		speed = P.speed;

		var sr = gameObject.AddComponent<SpriteRenderer>();
		sr.sprite = GenerateDotSprite();
		sr.sortingOrder = 1000;
		transform.localScale = Vector3.one * (P.radius * 2f);
		//Debug.Log($"target {this.target.name}");
	}

	void Update()
	{
		float dt = Time.deltaTime;
		life += dt; if (life > P.maxLife) { Expire(); }

		// °¡¼Ó
		speed = Mathf.Max(0f, speed + P.acceleration * dt);

		// Å¸±ê À¯È¿¼º È®ÀÎ + ÀçÅ¸±êÆÃ
		if (target == null && P.retargetOnLost)
			TryRetarget();

		Vector2 pos = transform.position;

		// ¿øÇÏ´Â ¹æÇâ(À¯µµ)
		//Vector2 desired = target ? ((Vector2)target.position - pos).normalized : dir;
		Vector2 desired = target != null && target.name == TargetingRuntimeUtil.AnchorName ? dir : ((Vector2)target.position - pos).normalized;
		float maxTurnRad = P.maxTurnDegPerSec * Mathf.Deg2Rad * dt;
		dir = Vector3.RotateTowards(dir, desired, maxTurnRad, 0f).normalized;

		// === ÀÌµ¿/Ãæµ¹(¿©·¯ ¹ø) Ã³¸® ===
		float remaining = speed * dt;

		while (remaining > 0f)
		{
			pos = transform.position;

			// 1) º® Ã¼Å©
			var wallHit = Physics2D.CircleCast(pos, P.radius, dir, remaining, P.blockerMask);
			if (wallHit.collider)
			{
				// º®±îÁö ÀÌµ¿ ÈÄ ¼Ò¸ê
				Move(wallHit.distance);
				Expire();
				return;
			}

			// 2) Àû Ã¼Å©
			var enemyHit = Physics2D.CircleCast(pos, P.radius, dir, remaining, P.enemyMask);
			if (enemyHit.collider)
			{
				var c = enemyHit.collider;

				// °°Àº ÄÝ¶óÀÌ´õ Áßº¹ Å¸°Ý ¹æÁö
				int id = c.GetInstanceID();
				if (!_hitIds.Contains(id))
				{
					// Ãæµ¹Á¡±îÁö ÀÌµ¿
					Move(enemyHit.distance);

					// ÇÇÇØ/³Ë¹é Àû¿ë
					if (c.TryGetComponent(out IVulnerable v))
						v.TakeDamage(P.damage, P.apRatio);
					if (c.attachedRigidbody)
						c.attachedRigidbody.AddForce(dir * P.knockback, ForceMode2D.Impulse);

					_hitIds.Add(id); // ±â·Ï

					// °üÅë ºÒ°¡ÀÌ°Å³ª(=¸íÁß Áï½Ã ¼Ò¸ê) / Å¸±ê ±× ÀÚÃ¼¸é ¼Ò¸ê
					if (!P.CanPenetrate || (target != null && c.transform == target))
					{
						Expire();
						return;
					}
				}
				else
				{
					// ÀÌ¹Ì ¸ÂÃá ´ë»óÀÌ¸é Ãæµ¹Á¡±îÁö´Â ±»ÀÌ ¾È ¸ØÃß°í Åë°ú Ã³¸®
					Move(enemyHit.distance);
				}

				// Ãæµ¹¸éÀ» »ìÂ¦ ³Ñ¾î°¡ ´ÙÀ½ Ä³½ºÆ®¿¡¼­ °°Àº ¸é¿¡ °É¸®Áö ¾Ê°Ô
				Move(SKIN);

				// ÀÜ¿© °Å¸® °»½Å
				remaining -= enemyHit.distance + SKIN;
				continue; // ´ÙÀ½ Ãæµ¹/ÀÌµ¿ Ã³¸®
			}

			// 3) Ãæµ¹ ¾øÀ¸¸é ³²Àº °Å¸®¸¸Å­ ÀÌµ¿ÇÏ°í Á¾·á
			Move(remaining);
			remaining = 0f;
		}

		// »ç°Å¸® Ã¼Å©
		if (traveled >= P.maxRange) Expire();
	}

	void Move(float d)
	{
		transform.position += (Vector3)(dir * d);
		traveled += d;
	}

	void TryRetarget()
	{
		var hits = Physics2D.OverlapCircleAll(transform.position, P.retargetRadius, P.enemyMask);
		float best = float.PositiveInfinity; Transform bestT = null;
		foreach (var h in hits)
		{
			float d = Vector2.SqrMagnitude((Vector2)h.bounds.center - (Vector2)transform.position);
			if (d < best) { best = d; bestT = h.transform; }
		}
		if (bestT) target = bestT;
	}

	Sprite GenerateDotSprite()
	{
		int s = 8; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
		var col = new Color32[s * s]; for (int i = 0; i < col.Length; i++) col[i] = new Color32(255, 255, 255, 255);
		tex.SetPixels32(col); tex.Apply();
		return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
	}

	public void Expire()
	{
		//Á¦°ÅµÉ ¶§ ¹º°¡ ÇØ¾ß ÇÑ´Ù?
		Destroy(gameObject);
	}
}