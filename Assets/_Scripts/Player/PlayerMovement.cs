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
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
        spriteRenderer = GetComponent<SpriteRenderer>(); 
    }

    private void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        movement = movement.normalized;

        anim.SetFloat("Horizontal", movement.x);
        anim.SetFloat("Vertical", movement.y);
        anim.SetFloat("Speed", movement.sqrMagnitude);

        if (movement != Vector2.zero)
        {
            anim.SetFloat("LastHorizontal", movement.x);
            anim.SetFloat("LastVertical", movement.y);
        }

        if (movement.x > 0)
        {
            spriteRenderer.flipX = false; 
        }
        else if (movement.x < 0)
        {
            spriteRenderer.flipX = true; 
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }
}