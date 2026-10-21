using UnityEngine;
using System.Collections;

public class ItemDrop : MonoBehaviour
{
    public ItemData itemData;
    public int amount;

    public float moveSpeed = 10f;
    public GameObject player;
    public ItemContainer itemContainer;

    private bool isMoving;
    private Rigidbody2D rb;

    [Header("Drop Effect")]
    public float jumpForce = 3f;
    public float horizontalForce = 2f;
    public float dropTime = 1f;
    public bool canCollect = false;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void PlayDropEffect()
    {
        float randomX = Random.Range(-horizontalForce, horizontalForce);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = new Vector2(randomX, jumpForce);

        StartCoroutine(StopDrop());
    }

    private IEnumerator StopDrop()
    {
        yield return new WaitForSeconds(dropTime);

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        canCollect = true;
    }

    public bool IsMoving => isMoving;

    public void StartMove(GameObject targetPlayer)
    {
        if (isMoving) return;

        player = targetPlayer;
        isMoving = true;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (!isMoving || player == null) return;
        if(canCollect == false) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.transform.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, player.transform.position) < 1f)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (itemContainer == null)
        {
            if (player != null)
            {
                PlayerStats ps = player.GetComponent<PlayerStats>();
                if (ps != null) itemContainer = ps.itemContainer;
            }
            if (itemContainer == null)
            {
                PlayerStats ps = FindFirstObjectByType<PlayerStats>();
                if (ps != null) itemContainer = ps.itemContainer;
            }
        }

        if (itemContainer != null && itemData != null)
        {
            if (!itemContainer.CanAddItem(itemData))
            {
                // Backpack full: Do not collect or destroy dropped item!
                isMoving = false;
                DamagePopupManager.ShowText(transform.position + Vector3.up * 0.6f, "Backpack Full!", new Color(1f, 0.4f, 0.4f), 3.0f);
                return;
            }

            bool added = itemContainer.AddItem(itemData, amount);
            if (!added)
            {
                isMoving = false;
                return;
            }

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayItemPickup(transform.position);
            }
        }

        Destroy(gameObject);
    }
}