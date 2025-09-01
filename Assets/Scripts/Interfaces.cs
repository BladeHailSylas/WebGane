using System;

namespace Interfaces
{
    public interface IVulnerable
    {
        void TakeDamage(float damage, UnityEngine.Vector2 hitPoint, float knockback, float apratio = 0, bool isfixed = false);
        void Die();
        bool isDead { get; }
    }

    public interface IMovable
    {
        void Move(UnityEngine.Vector2 direction, float speed);
        void Jump(float time);
    }

    public interface I
    {
        
    }
}
