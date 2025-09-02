using UnityEngine;
using ActInterfaces;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IMovable
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;

    private Vector2 moveInput;
    public void Move(Vector2 move, float velocity) 
    {
        //while (true)
        {
            rb.linearVelocity = move.normalized * velocity;
        }
    }
    public void Jump(float duration)
    {
        Console.WriteLine("Not implemented");
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Get input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
    }

    void FixedUpdate()
    {
        Move(moveInput, moveSpeed);
    }
}