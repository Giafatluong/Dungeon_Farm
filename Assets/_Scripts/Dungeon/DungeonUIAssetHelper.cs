using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Central helper to provide existing project visual assets (UI_Frame, UI_Slot, Golden Chest sprite, inventorySlot prefab)
/// to Dungeon UIs (RewardUI, DungeonEventUI, MerchantUI).
/// </summary>
public static class DungeonUIAssetHelper
{
    private static Sprite _frameSprite;
    private static Sprite _slotSprite;
    private static Sprite _toolbarSprite;
    private static Sprite _chestSprite;
    private static Sprite _bossSprite;
    private static Sprite _tentSprite;
    private static GameObject _slotPrefab;
    private static TMP_FontAsset _fontAsset;

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
                _slotPrefab = btn.gameObject;
                return _slotPrefab;
            }
        }

        LootUI lootUI = Object.FindFirstObjectByType<LootUI>(FindObjectsInactive.Include);
        if (lootUI != null)
        {
            InventoryButton btn = lootUI.GetComponentInChildren<InventoryButton>(true);
            if (btn != null)
            {
                _slotPrefab = btn.gameObject;
                return _slotPrefab;
            }
        }

        // 2. Search loaded assets
        GameObject[] allPrefabs = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allPrefabs.Length; i++)
        {
            if (allPrefabs[i] != null && allPrefabs[i].name == "inventorySlot")
            {
                _slotPrefab = allPrefabs[i];
                return _slotPrefab;
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

    public static void StyleSlotImage(Image img, Color? color = null)
    {
        if (img == null) return;
        Sprite slot = GetSlotSprite();
        if (slot != null)
        {
            img.sprite = slot;
            img.type = Image.Type.Simple;
            img.color = color ?? Color.white;
        }
        else
        {
            img.color = color ?? new Color(0.18f, 0.2f, 0.26f, 0.95f);
        }
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

    private static Sprite FindSprite(string spriteName)
    {
        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        // 1. Exact match
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name.Equals(spriteName, System.StringComparison.OrdinalIgnoreCase))
            {
                return sprites[i];
            }
        }
        // 2. Fallback prefix/contains match
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name.IndexOf(spriteName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return sprites[i];
            }
        }
        return null;
    }
}
