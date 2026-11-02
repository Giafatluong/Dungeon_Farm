using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural Pixel-Art & Vector Illustration Factory for Buff/Debuff Icons.
/// Generates crisp 32x32 stylized icons directly in memory without bloating the project repository.
/// Also supports loading custom imported Figma/art assets from Resources/BuffIcons/<key>.
/// </summary>
public static class BuffSpriteFactory
{
    private static readonly Dictionary<string, Sprite> _cache = new(System.StringComparer.OrdinalIgnoreCase);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _cache.Clear();
    }

    public static Sprite GetSprite(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (_cache.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        // 1. Check if user provided custom art exported from Figma in Resources/BuffIcons/
        Sprite custom = Resources.Load<Sprite>($"BuffIcons/{key}");
        if (custom != null)
        {
            _cache[key] = custom;
            return custom;
        }

        // 2. Generate crisp procedural illustration
        Sprite procedural = CreateProceduralIcon(key);
        if (procedural != null)
        {
            _cache[key] = procedural;
            return procedural;
        }

        return null;
    }

    private static Sprite CreateProceduralIcon(string key)
    {
        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        Color clear = new Color(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        Color bgInner = new Color(0.08f, 0.10f, 0.16f, 0.95f); // Dark midnight slate
        Color borderCol = GetBorderColorForKey(key);

        // Draw circular badge background
        float center = 15.5f;
        float outerR = 15.0f;
        float innerR = 12.8f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= outerR)
                {
                    if (dist > innerR)
                    {
                        // Border ring with gradient shine
                        float shine = (y - center) / outerR * 0.2f;
                        Color bc = borderCol + new Color(shine, shine, shine, 0f);
                        pixels[y * size + x] = bc;
                    }
                    else
                    {
                        // Inner dark badge
                        pixels[y * size + x] = bgInner;
                    }
                }
            }
        }

        // Draw specific stylized pixel illustration
        switch (key.ToLower())
        {
            case "atk_buff":
            case "attack_buff":
            case "strength":
                DrawSword(pixels, size, isBroken: false);
                break;

            case "atk_debuff":
            case "attack_debuff":
            case "frail":
            case "weak":
                DrawSword(pixels, size, isBroken: true);
                break;

            case "def_buff":
            case "defense_buff":
            case "shield":
            case "guard":
                DrawShield(pixels, size, isBroken: false);
                break;

            case "def_debuff":
            case "defense_debuff":
            case "corrode":
            case "vulnerable":
            case "armor_break":
                DrawShield(pixels, size, isBroken: true);
                break;

            case "speed_buff":
            case "agility":
            case "swift":
                DrawWingedBoot(pixels, size, isDebuff: false);
                break;

            case "speed_debuff":
            case "slow":
            case "sluggish":
                DrawWingedBoot(pixels, size, isDebuff: true);
                break;

            case "poison":
            case "decay":
            case "toxic":
                DrawPoisonDrop(pixels, size);
                break;

            case "health_buff":
            case "vitality":
            case "regen":
                DrawHeart(pixels, size);
                break;

            case "fullness_satiated":
            case "satiated":
            case "no_ne":
                DrawFeastBowl(pixels, size);
                break;

            case "fullness_hungry":
            case "hungry":
            case "starving":
                DrawEmptyHunger(pixels, size);
                break;

            case "fullness_overstuffed":
            case "overstuffed":
            case "boi_thuc":
                DrawOverstuffedFeast(pixels, size);
                break;

            case "blessing":
            case "divine":
                DrawDivineCrown(pixels, size);
                break;

            default:
                DrawGenericGem(pixels, size, borderCol);
                break;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }

    private static Color GetBorderColorForKey(string key)
    {
        return key.ToLower() switch
        {
            "atk_buff" or "attack_buff" or "strength" => new Color(1.0f, 0.28f, 0.22f, 1f), // Fiery crimson
            "atk_debuff" or "attack_debuff" or "weak" or "frail" => new Color(0.75f, 0.25f, 0.85f, 1f), // Dark magenta
            "def_buff" or "defense_buff" or "shield" or "guard" => new Color(0.15f, 0.65f, 1.0f, 1f), // Azure steel
            "def_debuff" or "defense_debuff" or "corrode" or "armor_break" => new Color(1.0f, 0.45f, 0.1f, 1f), // Acid orange
            "speed_buff" or "agility" or "swift" => new Color(0.12f, 0.90f, 0.65f, 1f), // Emerald breeze
            "speed_debuff" or "slow" or "sluggish" => new Color(0.55f, 0.45f, 0.38f, 1f), // Mud brown
            "poison" or "decay" or "toxic" => new Color(0.72f, 0.20f, 0.98f, 1f), // Toxic purple
            "health_buff" or "vitality" or "regen" => new Color(1.0f, 0.25f, 0.50f, 1f), // Ruby pink
            "fullness_satiated" or "satiated" or "no_ne" => new Color(0.20f, 0.88f, 0.35f, 1f), // Lime green
            "fullness_hungry" or "hungry" or "starving" => new Color(1.0f, 0.35f, 0.20f, 1f), // Warning orange
            "fullness_overstuffed" or "overstuffed" or "boi_thuc" => new Color(1.0f, 0.82f, 0.15f, 1f), // Topaz gold
            "blessing" or "divine" => new Color(1.0f, 0.95f, 0.45f, 1f), // Sunlight gold
            _ => new Color(0.5f, 0.7f, 1.0f, 1f)
        };
    }

    private static void SetPixel(Color[] pixels, int size, int x, int y, Color c)
    {
        if (x < 0 || x >= size || y < 0 || y >= size) return;
        pixels[y * size + x] = c;
    }

    #region Drawing Routines
    private static void DrawSword(Color[] p, int s, bool isBroken)
    {
        Color bladeEdge = new Color(0.7f, 0.75f, 0.85f, 1f);
        Color bladeCore = new Color(1.0f, 1.0f, 1.0f, 1f);
        Color guard = new Color(0.95f, 0.75f, 0.20f, 1f);
        Color grip = new Color(0.55f, 0.20f, 0.15f, 1f);
        Color aura = isBroken ? new Color(0.7f, 0.2f, 0.8f, 0.6f) : new Color(1f, 0.3f, 0.2f, 0.6f);

        // Diagonal blade from bottom-left (9,9) to top-right (23,23)
        for (int i = -3; i <= 9; i++)
        {
            if (isBroken && (i == 2 || i == 3)) continue; // crack in blade

            int bx = 16 + i;
            int by = 16 + i;
            if (isBroken && i > 3) { bx += 1; by -= 1; } // displaced broken tip

            SetPixel(p, s, bx, by, bladeCore);
            SetPixel(p, s, bx - 1, by, bladeEdge);
            SetPixel(p, s, bx, by + 1, bladeEdge);
            SetPixel(p, s, bx + 1, by, aura);
        }

        // Crossguard
        SetPixel(p, s, 12, 14, guard);
        SetPixel(p, s, 13, 13, guard);
        SetPixel(p, s, 14, 12, guard);
        SetPixel(p, s, 11, 15, guard);
        SetPixel(p, s, 15, 11, guard);

        // Grip & Pommel
        SetPixel(p, s, 11, 11, grip);
        SetPixel(p, s, 10, 10, grip);
        SetPixel(p, s, 9, 9, guard);
        SetPixel(p, s, 8, 8, guard);
    }

    private static void DrawShield(Color[] p, int s, bool isBroken)
    {
        Color border = new Color(0.85f, 0.90f, 1.0f, 1f);
        Color field = isBroken ? new Color(0.45f, 0.25f, 0.15f, 1f) : new Color(0.12f, 0.35f, 0.75f, 1f);
        Color emblem = new Color(1.0f, 0.82f, 0.20f, 1f);
        Color crack = new Color(1.0f, 0.3f, 0.1f, 1f);

        for (int y = 9; y <= 23; y++)
        {
            int width = y > 16 ? 6 : Mathf.RoundToInt(6.5f - (16 - y) * 0.7f);
            if (y < 12) width = 1;
            int cx = 15;

            for (int x = cx - width; x <= cx + width; x++)
            {
                bool isRim = (x == cx - width || x == cx + width || y == 23 || y == 9);
                if (isRim)
                {
                    SetPixel(p, s, x, y, border);
                }
                else
                {
                    // Center crest / emblem
                    bool isEmblem = (x == cx || (y == 16 && Mathf.Abs(x - cx) <= 2));
                    SetPixel(p, s, x, y, isEmblem ? emblem : field);
                }
            }
        }

        if (isBroken)
        {
            // Draw jagged fissure through shield
            SetPixel(p, s, 15, 23, crack);
            SetPixel(p, s, 14, 21, crack);
            SetPixel(p, s, 16, 19, crack);
            SetPixel(p, s, 15, 17, crack);
            SetPixel(p, s, 14, 15, crack);
            SetPixel(p, s, 16, 13, crack);
            SetPixel(p, s, 15, 11, crack);
        }
    }

    private static void DrawWingedBoot(Color[] p, int s, bool isDebuff)
    {
        Color boot = isDebuff ? new Color(0.45f, 0.40f, 0.35f, 1f) : new Color(0.95f, 0.70f, 0.20f, 1f);
        Color wing = isDebuff ? new Color(0.30f, 0.25f, 0.25f, 1f) : new Color(0.40f, 0.95f, 0.85f, 1f);
        Color sole = new Color(0.25f, 0.18f, 0.12f, 1f);

        // Boot leg and foot
        for (int y = 10; y <= 18; y++)
        {
            SetPixel(p, s, 14, y, boot);
            SetPixel(p, s, 15, y, boot);
        }
        for (int x = 11; x <= 18; x++)
        {
            SetPixel(p, s, x, 10, sole);
            SetPixel(p, s, x, 11, boot);
        }
        SetPixel(p, s, 18, 12, boot); // toe curve

        // Wing feathers extending back and up
        for (int w = 0; w < 3; w++)
        {
            int startX = 13 - w * 2;
            int startY = 15 + w * 2;
            for (int l = 0; l <= 4 - w; l++)
            {
                SetPixel(p, s, startX - l, startY + l / 2, wing);
            }
        }
    }

    private static void DrawPoisonDrop(Color[] p, int s)
    {
        Color poisonCore = new Color(0.55f, 0.95f, 0.35f, 1f); // Acid lime
        Color poisonDark = new Color(0.65f, 0.18f, 0.85f, 1f); // Deep venom violet
        Color bubble = new Color(0.9f, 1f, 0.7f, 1f);

        int cx = 15;
        for (int y = 8; y <= 22; y++)
        {
            int r = (y < 15) ? Mathf.RoundToInt((y - 7) * 0.7f) : Mathf.RoundToInt((22 - y) * 0.8f);
            for (int x = cx - r; x <= cx + r; x++)
            {
                float d = Mathf.Abs(x - cx);
                Color c = d <= 1 ? poisonCore : poisonDark;
                SetPixel(p, s, x, y, c);
            }
        }
        // Bubble highlights
        SetPixel(p, s, 14, 17, bubble);
        SetPixel(p, s, 16, 13, bubble);
    }

    private static void DrawHeart(Color[] p, int s)
    {
        Color heartCol = new Color(1.0f, 0.22f, 0.40f, 1f);
        Color shine = new Color(1.0f, 0.85f, 0.90f, 1f);

        int cx = 15;
        for (int y = 9; y <= 21; y++)
        {
            int w = (y <= 16) ? Mathf.RoundToInt((y - 8) * 0.7f) : Mathf.RoundToInt((21 - y) * 1.0f);
            if (y == 21) { SetPixel(p, s, cx - 3, 21, heartCol); SetPixel(p, s, cx + 3, 21, heartCol); continue; }
            if (y == 20) { SetPixel(p, s, cx - 4, 20, heartCol); SetPixel(p, s, cx - 3, 20, heartCol); SetPixel(p, s, cx + 3, 20, heartCol); SetPixel(p, s, cx + 4, 20, heartCol); continue; }

            for (int x = cx - w - 2; x <= cx + w + 2; x++)
            {
                SetPixel(p, s, x, y, heartCol);
            }
        }
        // Shine mark
        SetPixel(p, s, 12, 18, shine);
        SetPixel(p, s, 13, 19, shine);
    }

    private static void DrawFeastBowl(Color[] p, int s)
    {
        Color bowl = new Color(0.85f, 0.55f, 0.25f, 1f);
        Color soup = new Color(0.95f, 0.75f, 0.20f, 1f);
        Color carrot = new Color(1.0f, 0.45f, 0.15f, 1f);
        Color steam = new Color(0.6f, 0.95f, 0.7f, 0.7f);

        int cx = 15;
        // Bowl body
        for (int y = 9; y <= 15; y++)
        {
            int w = Mathf.RoundToInt((y - 7) * 1.0f);
            for (int x = cx - w; x <= cx + w; x++)
            {
                SetPixel(p, s, x, y, (y == 15) ? soup : bowl);
            }
        }
        // Vegetable chunks
        SetPixel(p, s, 13, 15, carrot);
        SetPixel(p, s, 16, 15, carrot);

        // Steam swirls
        SetPixel(p, s, 13, 18, steam);
        SetPixel(p, s, 14, 19, steam);
        SetPixel(p, s, 17, 18, steam);
        SetPixel(p, s, 16, 20, steam);
    }

    private static void DrawEmptyHunger(Color[] p, int s)
    {
        Color outline = new Color(0.9f, 0.35f, 0.2f, 1f);
        Color lightning = new Color(1.0f, 0.9f, 0.2f, 1f);

        // Hollow bowl or stomach ring
        int cx = 15;
        for (int y = 10; y <= 15; y++)
        {
            int w = Mathf.RoundToInt((y - 7) * 1.1f);
            SetPixel(p, s, cx - w, y, outline);
            SetPixel(p, s, cx + w, y, outline);
            if (y == 10) for (int x = cx - w; x <= cx + w; x++) SetPixel(p, s, x, y, outline);
        }
        // Hunger lightning in middle
        SetPixel(p, s, 15, 21, lightning);
        SetPixel(p, s, 14, 19, lightning);
        SetPixel(p, s, 16, 18, lightning);
        SetPixel(p, s, 14, 16, lightning);
    }

    private static void DrawOverstuffedFeast(Color[] p, int s)
    {
        Color gold = new Color(1.0f, 0.85f, 0.20f, 1f);
        Color meat = new Color(0.85f, 0.40f, 0.20f, 1f);
        Color bone = new Color(0.95f, 0.95f, 0.90f, 1f);

        // Huge roast drumstick
        int cx = 15;
        for (int y = 12; y <= 21; y++)
        {
            int r = Mathf.RoundToInt(4f - Mathf.Abs(y - 17) * 0.7f);
            for (int x = cx - r; x <= cx + r; x++)
            {
                SetPixel(p, s, x, y, meat);
            }
        }
        // Bone handle
        SetPixel(p, s, 15, 11, bone);
        SetPixel(p, s, 15, 10, bone);
        SetPixel(p, s, 14, 9, bone);
        SetPixel(p, s, 16, 9, bone);

        // Golden fortress sparkles around feast
        SetPixel(p, s, 9, 17, gold);
        SetPixel(p, s, 22, 17, gold);
        SetPixel(p, s, 15, 23, gold);
    }

    private static void DrawDivineCrown(Color[] p, int s)
    {
        Color gold = new Color(1.0f, 0.90f, 0.25f, 1f);
        Color gem = new Color(0.2f, 0.8f, 1.0f, 1f);
        Color ray = new Color(1.0f, 0.98f, 0.75f, 0.8f);

        // 3-Pointed Crown
        int cx = 15;
        for (int x = 10; x <= 21; x++)
        {
            SetPixel(p, s, x, 12, gold);
            SetPixel(p, s, x, 13, gold);
        }
        // Left point
        for (int y = 14; y <= 18; y++) SetPixel(p, s, 10 + (y - 14) / 3, y, gold);
        // Right point
        for (int y = 14; y <= 18; y++) SetPixel(p, s, 21 - (y - 14) / 3, y, gold);
        // Middle tall point
        for (int y = 14; y <= 20; y++) SetPixel(p, s, cx, y, gold);

        // Crown Jewels
        SetPixel(p, s, cx, 13, gem);
        SetPixel(p, s, 10, 19, gem);
        SetPixel(p, s, 21, 19, gem);

        // Radiant halo aura
        SetPixel(p, s, cx, 23, ray);
        SetPixel(p, s, cx - 4, 22, ray);
        SetPixel(p, s, cx + 4, 22, ray);
    }

    private static void DrawGenericGem(Color[] p, int s, Color col)
    {
        int cx = 15;
        for (int y = 11; y <= 20; y++)
        {
            int r = Mathf.RoundToInt(4f - Mathf.Abs(y - 15.5f) * 0.8f);
            for (int x = cx - r; x <= cx + r; x++)
            {
                SetPixel(p, s, x, y, col);
            }
        }
    }
    #endregion
}

