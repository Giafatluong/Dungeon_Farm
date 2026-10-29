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
    private float collectCooldownUntil;

    [Header("Drop Effect")]
    public float jumpForce = 3f;
    public float horizontalForce = 2f;
    public float dropTime = 1f;
    public bool canCollect = false;

    public bool IsMoving => isMoving;
    public bool CanStartMove => canCollect && !isMoving && Time.time >= collectCooldownUntil;

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

    public void StartMove(GameObject targetPlayer)
    {
        if (isMoving || Time.time < collectCooldownUntil) return;

        player = targetPlayer;
        isMoving = true;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (!isMoving || player == null) return;
        if (!canCollect) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.transform.position,
            moveSpeed * Time.deltaTime
        );

        if ((transform.position - player.transform.position).sqrMagnitude < 1f)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (itemContainer == null)
        {
            if (PlayerStats.Instance != null && PlayerStats.Instance.itemContainer != null)
            {
                itemContainer = PlayerStats.Instance.itemContainer;
            }
            else if (player != null && player.TryGetComponent(out PlayerStats ps))
            {
                itemContainer = ps.itemContainer;
            }
            else
            {
                PlayerStats foundPs = FindFirstObjectByType<PlayerStats>();
                if (foundPs != null) itemContainer = foundPs.itemContainer;
            }
        }

        if (itemContainer != null && itemData != null)
        {
            if (!itemContainer.CanAddItem(itemData))
            {
                // Backpack full: Do not collect or destroy dropped item; cooldown prevents per-frame popup spam
                isMoving = false;
                collectCooldownUntil = Time.time + 1.5f;
                DamagePopupManager.ShowText(transform.position + Vector3.up * 0.6f, "Backpack Full!", new Color(1f, 0.4f, 0.4f), 3.0f);
                return;
            }

            bool added = itemContainer.AddItem(itemData, amount);
            if (!added)
            {
                isMoving = false;
                collectCooldownUntil = Time.time + 1.5f;
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