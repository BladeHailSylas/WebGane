using ActInterfaces;
using UnityEngine;

public class PlayerLocomotion : MonoBehaviour, IMovable, IPullable
{
    [SerializeField] private Rigidbody2D rb;          // Kinematic ��ü
    public Vector2 LastMoveDir { get; private set; }
    // Knockback�� Kinematic���� �����ӵ��� �ƴ� "�߰� ����"�� ó��
    Vector2 _knockbackBudget;                         // �̹� �����ӿ� �Һ��� �߰� ����(���� ��ǥ)

    /// <summary>
    /// IMovable: �ǵ�(���⡤�ӵ�)�� ���� ������ "��Ÿ"�� ȯ���Ͽ� Motor�� �����մϴ�.
    /// - �̵� ������ ��������(�ӵ�/�Է�), �浹-������ Motor����.
    /// - �����Ӵ� �� �� �� ȣ��ǵ��� ��Ʈ�ѷ�(Update)������ ȣ���ϼ���.
    /// </summary>
    public void Move(Vector2 direction, Rigidbody2D rbArg, float speed)
    {
        // ���� ����ȭ �� ��Ÿ ����
        Vector2 dir = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector2.zero;
        Vector2 delta = Mathf.Max(0f, speed) * Time.deltaTime * dir;
        // Knockback �߰� ������ ���� �����ӿ� �Һ� �� 0����
        if (_knockbackBudget.sqrMagnitude > 0f)
        {
            delta += _knockbackBudget;
            _knockbackBudget = Vector2.zero;
        }
		// �ǵ� ������ ��ȣ �������� �Ͽ� ��ħ û��(�𼭸� �� ����)  // :contentReference[oaicite:12]{index=12}
		var motor = GetComponentInParent<KinematicMotor2D>();
		if (!motor) return;
		//motor.RemoveComponent();
		motor.Depenetration();
        // ���� ���� �̵�(�浹�� ����/�����̵�� Motor ��å�� ����)      // :contentReference[oaicite:13]{index=13}
        var res = motor.SweepMove(delta);
		motor.Depenetration();
        // ������ ���� �̵� ���� ���(���Ѵٸ� ���� �ӵ� �� 2�� �Ļ� ����)
        LastMoveDir = direction;//motor.LastMoveVector;
    }

    /// <summary>
    /// IPullable: Kinematic������ velocity ������ ���ǹ��ϹǷ�,
    /// "��� �� �� �и��� �߰� ����" �������� ��ȯ�� ���� Move���� �Һ��մϴ�.
    /// force ������ '�Ÿ�'�� ����(�ʿ� �� ����/�ð�������� Ȯ�� ����).
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        Vector2 dir = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector2.zero;
        _knockbackBudget += dir * Mathf.Max(0f, force);
    }

    // (����) ���� Jump/Coroutine�� �״�� �ε�, ���� ���� �̵��� �ʿ��ϸ� ���� ����/���̾�� �и� ���� -> Jump�� ��� ����ؾ� ���� �𸣰���
}