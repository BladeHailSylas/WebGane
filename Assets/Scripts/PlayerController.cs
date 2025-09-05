using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerActs acts;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerAttackController atkctrl;
    private Rigidbody2D rb;
    public void MakeMove(Vector2 mover)
    {
        if (mover != null)
        {
            acts.Move(mover, rb);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    /*void Update()
    {
        // Get input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
    }*/
}