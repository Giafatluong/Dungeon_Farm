using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Central helper providing original project pixel-art visual assets (UI_Frame, UI_Slot, UI_Toolbar)
/// across Base & Dungeon UIs (KitchenUI, StatueUI, MerchantUI, LootUI, CombatUI, etc.).
/// </summary>
public static class DungeonUIAssetHelper
{
    public enum ButtonStyle
    {
        PrimaryCook,
        Attack,
        Defend,
        Eat,
        EndTurn,
        Victory,
        WoodRustic
    }

    public enum PlaqueStyle
    {
        Gold,
        Victory,
        Defeat,
        Wood
    }

    public enum PanelStyle
    {
        Wood,
        Slate,
        Parchment
    }

    #region Cached Sprites
    private static Sprite _frameSprite;
    private static Sprite _slotSprite;
    private static Sprite _toolbarSprite;
    private static Sprite _chestSprite;
    private static Sprite _bossSprite;
    private static Sprite _tentSprite;
    private static GameObject _slotPrefab;
    private static TMP_FontAsset _fontAsset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        _frameSprite = null;
        _slotSprite = null;
        _toolbarSprite = null;
        _chestSprite = null;
        _bossSprite = null;
        _tentSprite = null;
        _slotPrefab = null;
        _fontAsset = null;
    }
    #endregion

    #region Sprite Accessors (Original Pixel-Art Assets)
    public static Sprite GetFrameSprite()
    {
        if (_frameSprite != null) return _frameSprite;
        _frameSprite = FindSprite("UI_Frame");
        return _frameSprite;
    }

    public static Sprite GetSlotSprite()
    {
        if (_slotSprite != null) return _slotSprite;
        _slotSprite = FindSprite("UI_Slot");
        return _slotSprite;
    }

    public static Sprite GetToolbarSprite()
    {
        if (_toolbarSprite != null) return _toolbarSprite;
        _toolbarSprite = FindSprite("UI_Toolbar");
        return _toolbarSprite;
    }

    public static Sprite GetPanelSprite(PanelStyle style = PanelStyle.Wood)
    {
        return GetFrameSprite();
    }

    public static Sprite GetPlaqueSprite(PlaqueStyle style = PlaqueStyle.Gold)
    {
        return GetToolbarSprite();
    }

    public static Sprite GetButtonSprite(ButtonStyle style = ButtonStyle.WoodRustic)
    {
        return GetFrameSprite();
    }

    public static Sprite GetCozySlotSprite()
    {
        return GetSlotSprite();
    }

    public static Sprite GetSelectedSlotSprite()
    {
        return GetSlotSprite();
    }

    public static Sprite GetHpFillSprite()
    {
        return null;
    }

    public static Sprite GetHungerFillSprite()
    {
        return null;
    }

    public static Sprite GetApFillSprite()
    {
        return null;
    }

    public static Sprite GetChestSprite()
    {
        if (_chestSprite != null) return _chestSprite;
        _chestSprite = FindSprite("Golden_Chest_Anim_0") 
                    ?? FindSprite("Golden_Chest_Anim_4") 
                    ?? FindSprite("TX Props Chest Opened")
                    ?? FindSprite("Chest_Anim_0");
        return _chestSprite;
    }

    public static Sprite GetBossSprite()
    {
        if (_bossSprite != null) return _bossSprite;
        _bossSprite = FindSprite("Boss_Idle_0")
                   ?? FindSprite("Boss_Idle")
                   ?? FindSprite("FE_Idle_0")
                   ?? FindSprite("Boss_Roar_0");
        return _bossSprite;
    }

    public static Sprite GetTentSprite()
    {
        if (_tentSprite != null) return _tentSprite;
        _tentSprite = FindSprite("Tent_Small")
                   ?? FindSprite("Tent_Big")
                   ?? FindSprite("Campfire");
        return _tentSprite;
    }
    #endregion

    #region Prefabs & Fonts
    public static GameObject GetSlotPrefab()
    {
        if (_slotPrefab != null) return _slotPrefab;

        // 1. Try finding from active or inactive scene components
        InventoryPanel invPanel = Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        if (invPanel != null)
        {
            InventoryButton btn = invPanel.GetComponentInChildren<InventoryButton>(true);
            if (btn != null)
            {
                return btn.gameObject;
            }
        }

        RewardUI rewardUI = Object.FindFirstObjectByType<RewardUI>(FindObjectsInactive.Include);
        if (rewardUI != null)
        {
            InventoryButton btn = rewardUI.GetComponentInChildren<InventoryButton>(true);
            if (btn != null)
            {
                return btn.gameObject;
            }
        }

        // 2. Search loaded assets
        GameObject[] allPrefabs = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allPrefabs.Length; i++)
        {
            if (allPrefabs[i] != null && allPrefabs[i].name == "inventorySlot")
            {
                if (allPrefabs[i].scene.name == null)
                {
                    _slotPrefab = allPrefabs[i];
                }
                return allPrefabs[i];
            }
        }

        return null;
    }

    public static TMP_FontAsset GetFontAsset()
    {
        if (_fontAsset != null) return _fontAsset;
        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < allFonts.Length; i++)
        {
            if (allFonts[i] != null && allFonts[i].name.Contains("LiberationSans"))
            {
                _fontAsset = allFonts[i];
                return _fontAsset;
            }
        }
        if (allFonts.Length > 0) _fontAsset = allFonts[0];
        return _fontAsset;
    }

    public static void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        TMP_FontAsset font = GetFontAsset();
        if (font != null)
        {
            tmp.font = font;
        }
    }
    #endregion

    #region Styling Methods (Original Pixel-Art Styling)
    public static void StyleSlicedPanel(Image img, PanelStyle style = PanelStyle.Wood)
    {
        if (img == null) return;
        Sprite frame = GetFrameSprite();
        if (frame != null)
        {
            img.sprite = frame;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        }
    }

    public static void StylePlaque(Image img, PlaqueStyle style = PlaqueStyle.Gold)
    {
        if (img == null) return;
        Sprite toolbar = GetToolbarSprite();
        if (toolbar != null)
        {
            img.sprite = toolbar;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = Color.white;
        }
    }

    public static void StyleSlotImage(Image img, bool isSelected = false)
    {
        if (img == null) return;
        Sprite slot = GetSlotSprite();
        if (slot != null)
        {
            img.sprite = slot;
            img.type = Image.Type.Sliced;
            img.color = isSelected ? new Color(1f, 0.9f, 0.5f, 1f) : Color.white;
        }
        else
        {
            img.color = isSelected ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.18f, 0.2f, 0.26f, 0.95f);
        }
    }

    public static void StyleSlotImage(Image img, Color? color = null)
    {
        if (img == null) return;
        Sprite slot = GetSlotSprite();
        if (slot != null)
        {
            img.sprite = slot;
            img.type = Image.Type.Sliced;
            img.color = color ?? Color.white;
        }
        else
        {
            img.color = color ?? new Color(0.18f, 0.2f, 0.26f, 0.95f);
        }
    }

    public static void StyleSlicedFrame(Image img, Color color)
    {
        if (img == null) return;
        Sprite frame = GetFrameSprite();
        if (frame != null)
        {
            img.sprite = frame;
            img.type = Image.Type.Sliced;
            img.color = color;
        }
        else
        {
            img.color = color;
        }
    }

    public static void StyleTactileButton(Button btn, Image img, ButtonStyle style)
    {
        Color btnColor = style switch
        {
            ButtonStyle.Victory => new Color(0.28f, 0.65f, 0.35f, 1f),
            ButtonStyle.Attack => new Color(0.78f, 0.28f, 0.28f, 1f),
            ButtonStyle.Defend => new Color(0.28f, 0.48f, 0.78f, 1f),
            ButtonStyle.Eat => new Color(0.85f, 0.58f, 0.22f, 1f),
            ButtonStyle.PrimaryCook => new Color(0.88f, 0.48f, 0.16f, 1f),
            ButtonStyle.EndTurn => new Color(0.38f, 0.38f, 0.48f, 1f),
            _ => Color.white
        };

        StyleButton(btn, img, btnColor);
    }

    public static void StyleButton(Button btn, Image img, Color normalColor, Color? hoverColor = null, Color? pressedColor = null)
    {
        if (btn == null) return;
        if (img != null)
        {
            StyleSlicedFrame(img, normalColor);
            btn.targetGraphic = img;
        }

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = hoverColor ?? new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = pressedColor ?? new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        cb.colorMultiplier = 1f;
        btn.colors = cb;
    }
    #endregion

    #region Utilities
    private static Sprite FindSprite(string spriteName)
    {
        // 1. Direct memory scan pass for loaded sprites
        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name.Equals(spriteName, System.StringComparison.OrdinalIgnoreCase))
            {
                return sprites[i];
            }
        }

#if UNITY_EDITOR
        // 2. Editor AssetDatabase pass (finds sprites on disk if not preloaded)
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"{spriteName} t:Sprite");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite edSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (edSprite != null && edSprite.name.Equals(spriteName, System.StringComparison.OrdinalIgnoreCase))
            {
                return edSprite;
            }
        }
#endif

        // 3. Fallback contains match
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name.IndexOf(spriteName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return sprites[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Tìm Canvas chính trên màn hình (Screen Space Overlay / Camera) thay vì nhầm lẫn sang World Space Canvas (như HealthBarCanvas).
    /// </summary>
    public static Canvas GetMainScreenCanvas()
    {
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allCanvases.Length; i++)
        {
            if (allCanvases[i] != null && allCanvases[i].name.Equals("CombatCanvas", System.StringComparison.OrdinalIgnoreCase))
                return allCanvases[i];
        }
        for (int i = 0; i < allCanvases.Length; i++)
        {
            if (allCanvases[i] != null && allCanvases[i].renderMode != RenderMode.WorldSpace)
                return allCanvases[i];
        }
        return allCanvases.Length > 0 ? allCanvases[0] : null;
    }

    /// <summary>
    /// Tạo thanh cuộn slider dọc cho ScrollRect.
    /// </summary>
    public static Scrollbar CreateVerticalScrollbar(Transform parent, ScrollRect scrollRect, Color handleColor, float width = 12f)
    {
        if (parent == null) return null;

        GameObject sbGO = new("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.transform.SetParent(parent, false);

        RectTransform sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.sizeDelta = new Vector2(width, 0);
        sbRT.anchoredPosition = Vector2.zero;

        Image sbBg = sbGO.GetComponent<Image>();
        sbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.85f);

        Scrollbar sb = sbGO.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        GameObject slideAreaGO = new("SlidingArea", typeof(RectTransform));
        slideAreaGO.transform.SetParent(sbGO.transform, false);
        RectTransform saRT = slideAreaGO.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.sizeDelta = Vector2.zero;

        GameObject handleGO = new("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(slideAreaGO.transform, false);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = Vector2.zero;

        Image handleImg = handleGO.GetComponent<Image>();
        StyleSlicedFrame(handleImg, handleColor);

        sb.handleRect = handleRT;
        sb.targetGraphic = handleImg;

        if (scrollRect != null)
        {
            scrollRect.verticalScrollbar = sb;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        return sb;
    }
    #endregion
}
