// AimLine.cs
// - �÷��̾� ��ġ���� ���콺 ���� ��ǥ���� ���� �׸��ϴ�.
// - ��ֹ� ���̾ �����ϸ� Raycast�� �� ���̸� �� �������� "Ŭ����"�� �� �ֽ��ϴ�.
// - URP/2D ������Ʈ������ �ٷ� ��� ����.
// AimLine�� ���� ������ ����� �ִ� ��Ÿ��� ������ �ִ� ������ �ϸ� �� �ɱ�?
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class AimLineSider : MonoBehaviour
{
    [Header("Line Settings")]
    public float width = 0.05f;          // �� ����
    public float maxLength = 1.2f;         // �� �ִ� ����
    public Color color = new(0.3f, 0.3f, 0.3f, 0.4f); // �� ��
    public float degree;
    public bool isLeft = false;

    [Header("Obstacles (Optional)")]
    public bool clampToObstacle = false; // true�� ��ֹ����� ���� �ڸ��ϴ�.
    public LayerMask obstacleMask;       // ��ֹ� ���̾� ����ũ

    [Header("Sorting (2D draw order)")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 50;        // ��ü���� ���� ���̵��� ����� ����

    private LineRenderer _lr;
    private Camera _cam;

    void Awake()
    {
        _cam = Camera.main;

        // �ڽ� ������Ʈ�� LineRenderer�� ���� �ǰ�, ���� ������Ʈ�� �߰��ص� �˴ϴ�.
        _lr = gameObject.AddComponent<LineRenderer>();

        // 2D���� ���� ���� ���� ĸ/�β� ����
        _lr.positionCount = 2;
        _lr.useWorldSpace = true;
        _lr.numCapVertices = 8;
        _lr.numCornerVertices = 0;

        _lr.startWidth = width;
        _lr.endWidth = width;

        _lr.startColor = color;
        _lr.endColor = color;

        // ��Ƽ����: �⺻ Unlit �迭�̸� ����մϴ�(���� ���� OK).
        // URP ������Ʈ��� "Sprites/Default" �Ǵ� "Universal Render Pipeline/Unlit" �� ���� �� ���.
        var mat = new Material(Shader.Find("Sprites/Default"));
        _lr.material = mat;

        // 2D ���� (��������Ʈ�� ��ĥ �� ����)
        _lr.sortingLayerName = sortingLayerName;
        _lr.sortingOrder = sortingOrder;

        obstacleMask = LayerMask.GetMask("Walls");
    }

    void Update()
    {
        if (_cam == null) return;

        // 1) ������: �÷��̾�(���� ������Ʈ) ��ġ
        Vector3 start = transform.position;

        // 2) ���콺 ��ũ����ǥ �� ������ǥ
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = start.z; // 2D ���(z ����) ����

        // 3) �ִ� ���� ����
        Vector3 dir = (mouseWorld - start);
        float dist = dir.magnitude;
        if (dist > Mathf.Epsilon) dir /= dist;

        float length = Mathf.Min(dist, maxLength);
        Vector3 end = start + dir * length;

        // 4) ��ֹ��� �ε����� �� ���������� �߶󳻱�(����)
        if (clampToObstacle)
        {
            var hit = Physics2D.Raycast(start, dir, length, obstacleMask);
            if (hit.collider != null)
                end = hit.point;
        }

        // 5) �� ��ǥ ����
        _lr.SetPosition(0, start);
        _lr.SetPosition(1, end);

        // (�ɼ�) ���콺 ������ ��ư�� ���� ���� ���̰�
        // _lr.enabled = Input.GetMouseButton(1);
    }
}