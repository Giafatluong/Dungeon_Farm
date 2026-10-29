using UnityEngine;

public class BedInteraction : MonoBehaviour, IInteractable
{
    [Header("References")]
    public DayManager dayManager;

    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from the bed collider for the player to sleep")]
    public float maxInteractDistance = 2.0f;

    private Collider2D cachedCol;
    public Collider2D CachedCollider => cachedCol != null ? cachedCol : (cachedCol = GetComponent<Collider2D>());

    private void Awake()
    {
        cachedCol = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance != null ? DayManager.Instance : FindFirstObjectByType<DayManager>();
        }
    }

    /// <summary>
    /// Checks if the player position is physically within range of this bed.
    /// </summary>
    public bool IsPlayerInRange(Vector3 playerPosition)
    {
        Collider2D col = CachedCollider;
        Vector2 bedPoint = (col != null) ? col.ClosestPoint(playerPosition) : (Vector2)transform.position;
        return Vector2.Distance(bedPoint, playerPosition) <= maxInteractDistance;
    }

    public void Interact()
    {
        // Must be in physical range of player to sleep
        PlayerMovement pm = PlayerMovement.Instance != null ? PlayerMovement.Instance : FindFirstObjectByType<PlayerMovement>();
        if (pm != null && !IsPlayerInRange(pm.transform.position))
        {
            return;
        }

        if (UIEscapeManager.IsAnyModalUIOpen())
            return;

        DayManager targetDayManager = (DayManager.Instance != null) ? DayManager.Instance : dayManager;
        if (targetDayManager == null)
        {
            targetDayManager = FindFirstObjectByType<DayManager>();
        }

        if (targetDayManager != null)
        {
            dayManager = targetDayManager;
            targetDayManager.Sleep();
        }
        else
        {
            Debug.LogWarning("[BedInteraction] DayManager not found! Cannot sleep.");
        }
    }
}
