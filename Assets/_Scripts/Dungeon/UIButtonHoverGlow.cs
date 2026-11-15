using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Universal high-visibility UI component for buttons and interactive controls:
/// - Distinct, bold glowing golden outline (3px) on hover.
/// - Surface illumination (brightness boost on hover, tactile press dim on click).
/// - Enforces strictly fixed 1.0x scale (eliminating all shrinking / squashing).
/// - Disables SpriteSwap and separate hover assets permanently.
/// - Automatically auto-attaches to all UI Buttons across all scenes and canvases.
/// </summary>
[DisallowMultipleComponent]
public class UIButtonHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Header("Outline Configuration")]
    [SerializeField] private Outline outline;
    [SerializeField] private Color hoverOutlineColor = new Color(1.0f, 0.95f, 0.42f, 1.0f); // Brilliant sunlight gold
    [SerializeField] private Vector2 outlineDistance = new Vector2(3f, -3f);

    private Selectable selectable;
    private Image buttonImage;
    private Color originalColor = Color.white;
    private bool hasOriginalColor = false;
    private bool isHovered = false;
    private bool isPressed = false;
    private bool isSelected = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoRegisterSceneButtons()
    {
        SceneManager.sceneLoaded += (scene, mode) => AttachToAllButtons();
        AttachToAllButtons();
    }

    /// <summary>
    /// Scans and attaches UIButtonHoverGlow to all active and inactive Buttons in current scenes.
    /// </summary>
    public static void AttachToAllButtons()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            if (b != null) Ensure(b);
        }
    }

    /// <summary>
    /// Ensures a button has UIButtonHoverGlow attached and initialized.
    /// </summary>
    public static UIButtonHoverGlow Ensure(Button btn)
    {
        if (btn == null) return null;
        // Inventory slots have specialized state handling in InventoryButton
        if (btn.GetComponent<InventoryButton>() != null) return null;

        UIButtonHoverGlow glow = btn.GetComponent<UIButtonHoverGlow>();
        if (glow == null)
        {
            glow = btn.gameObject.AddComponent<UIButtonHoverGlow>();
        }
        return glow;
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        selectable = GetComponent<Selectable>();
        buttonImage = GetComponent<Image>();

        if (buttonImage != null && !hasOriginalColor)
        {
            originalColor = buttonImage.color;
            hasOriginalColor = true;
        }

        if (selectable != null)
        {
            // Disable SpriteSwap so it never swaps to distorted/squashed hover assets
            if (selectable.transition == Selectable.Transition.SpriteSwap)
            {
                selectable.transition = Selectable.Transition.None;
                selectable.spriteState = default;
            }
        }

        if (buttonImage != null)
        {
            // Disable preserveAspect to prevent aspect-ratio squashing inside fixed frames
            buttonImage.preserveAspect = false;
        }

        if (outline == null)
        {
            outline = GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
            }
        }

        if (outline != null)
        {
            outline.effectDistance = outlineDistance;
            outline.useGraphicAlpha = true;
            outline.enabled = false;
        }

        // Strictly enforce 1.0x scale
        transform.localScale = Vector3.one;

        UpdateVisual();
    }

    private void OnEnable()
    {
        transform.localScale = Vector3.one;
        isHovered = false;
        isPressed = false;
        isSelected = false;

        if (buttonImage != null && !hasOriginalColor)
        {
            originalColor = buttonImage.color;
            hasOriginalColor = true;
        }

        UpdateVisual();
    }

    private void OnDisable()
    {
        isHovered = false;
        isPressed = false;
        isSelected = false;
        if (outline != null) outline.enabled = false;
        if (buttonImage != null && hasOriginalColor)
        {
            buttonImage.color = originalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        isHovered = true;
        UpdateVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        UpdateVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        isPressed = true;
        transform.localScale = Vector3.one;
        UpdateVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        transform.localScale = Vector3.one;
        UpdateVisual();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        UpdateVisual();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        UpdateVisual();
    }

    public void SetOriginalColor(Color color)
    {
        originalColor = color;
        hasOriginalColor = true;
        UpdateVisual();
    }

    public void RefreshOutline()
    {
        if (selectable == null || outline == null)
        {
            Initialize();
        }
        UpdateVisual();
    }

    private void LateUpdate()
    {
        // Guard against any external scripts or animations attempting to alter button scale
        if (transform.localScale != Vector3.one)
        {
            transform.localScale = Vector3.one;
        }
    }

    private void UpdateVisual()
    {
        bool interactable = selectable == null || selectable.interactable;
        bool active = (isHovered || isSelected) && interactable;

        if (outline != null)
        {
            if (active)
            {
                outline.enabled = true;
                outline.effectColor = hoverOutlineColor;
                outline.effectDistance = outlineDistance;
            }
            else
            {
                outline.enabled = false;
            }
        }

        if (buttonImage != null && hasOriginalColor)
        {
            if (!interactable)
            {
                buttonImage.color = originalColor * 0.6f;
            }
            else if (isPressed)
            {
                // Tactile feedback on click
                buttonImage.color = originalColor * 0.85f;
            }
            else if (active)
            {
                // Surface illumination on hover
                if (originalColor.r >= 0.85f && originalColor.g >= 0.85f && originalColor.b >= 0.85f)
                {
                    buttonImage.color = new Color(1.25f, 1.25f, 1.15f, originalColor.a);
                }
                else
                {
                    buttonImage.color = Color.Lerp(originalColor, Color.white, 0.32f);
                }
            }
            else
            {
                buttonImage.color = originalColor;
            }
        }
    }
}
