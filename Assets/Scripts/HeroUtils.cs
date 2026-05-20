using UnityEngine;

public static class HeroUtils
{
    // --- Combat Power (CP) ---
    public static int CalculateCP(HeroInstance h)
    {
        float primary;
        float secondary;

        switch (h.heroClass)
        {
            // Melee branch — STR primary
            case HeroClass.Vanguard:
            case HeroClass.Guardian:
            case HeroClass.Gladiator:
            case HeroClass.Immortal:
                primary   = h.currentSTR * 2.0f;
                secondary = h.currentHP + h.currentINT + h.currentAGI;
                break;

            // Agility branch — AGI primary
            case HeroClass.Scout:
            case HeroClass.Assassin:
            case HeroClass.Ranger:
            case HeroClass.ShadowReaper:
                primary   = h.currentAGI * 2.0f;
                secondary = h.currentSTR + h.currentHP + h.currentINT;
                break;

            // Magic branch — INT primary
            case HeroClass.Acolyte:
            case HeroClass.Elementalist:
            case HeroClass.BloodMage:
            case HeroClass.HighSage:
                primary   = h.currentINT * 2.0f;
                secondary = h.currentSTR + h.currentHP + h.currentAGI;
                break;

            // Novice — equal weight
            default:
                primary   = (h.currentSTR + h.currentINT + h.currentAGI) / 3.0f;
                secondary = h.currentHP;
                break;
        }

        float levelMult = 1f + (h.level * 0.1f);
        return Mathf.RoundToInt((primary + secondary) * levelMult);
    }

    // --- Fatigue State ---
    public enum FatigueState { Fresh, Strained, Breaking }

    public static FatigueState GetFatigueState(HeroInstance h)
    {
        if (h.fatigue >= 75) return FatigueState.Breaking;
        if (h.fatigue >= 50) return FatigueState.Strained;
        return FatigueState.Fresh;
    }

    // --- Class Tier Label ---
    public static string GetClassLabel(HeroClass c)
    {
        return c.ToString().ToUpper(); // e.g. "VANGUARD", "SHADOW REAPER"
    }

    public static int GetClassTier(HeroClass c)
    {
        switch (c)
        {
            case HeroClass.Novice:                          return 0;
            case HeroClass.Vanguard:
            case HeroClass.Scout:
            case HeroClass.Acolyte:                        return 1;
            case HeroClass.Guardian:
            case HeroClass.Gladiator:
            case HeroClass.Assassin:
            case HeroClass.Ranger:
            case HeroClass.Elementalist:
            case HeroClass.BloodMage:                      return 2;
            case HeroClass.Immortal:
            case HeroClass.ShadowReaper:
            case HeroClass.HighSage:                       return 3;
            default:                                        return 0;
        }
    }
}
