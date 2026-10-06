using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator anim;
    
    private SpriteRenderer spriteRenderer; 

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        movement = movement.normalized;

        if (anim != null)
        {
            anim.SetFloat("Horizontal", movement.x);
            anim.SetFloat("Vertical", movement.y);
            anim.SetFloat("Speed", movement.sqrMagnitude);

            if (movement != Vector2.zero)
            {
                anim.SetFloat("LastHorizontal", movement.x);
                anim.SetFloat("LastVertical", movement.y);
            }
        }

        if (spriteRenderer != null)
        {
            if (movement.x > 0)
            {
                spriteRenderer.flipX = false; 
            }
            else if (movement.x < 0)
            {
                spriteRenderer.flipX = true; 
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
        }
    }
}