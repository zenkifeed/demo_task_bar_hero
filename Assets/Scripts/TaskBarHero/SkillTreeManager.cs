using UnityEngine;

public enum SkillBranch
{
    CodeOptimization = 0,  // ATK
    CaffeineReserve = 1,   // Max HP
    KeyboardShortcuts = 2, // Attack speed
    OvertimePay = 3,       // Gold
    StackOverflow = 4      // XP
}

// Gold-spend upgrade tree: 5 branches x 8 levels, permanent multiplicative
// buffs to Hero combat stats. Level tracking only — IdleCombatManager owns
// the gold transaction and applies the multipliers/capstones during combat.
public class SkillTreeManager : MonoBehaviour
{
    public const int MaxLevel = 8;
    public const int BranchCount = 5;

    public static readonly string[] DisplayNames =
    {
        "Code Optimization", "Caffeine Reserve", "Keyboard Shortcuts", "Overtime Pay", "Stack Overflow"
    };

    public static readonly string[] FlavorText =
    {
        "+5% ATK / level", "+8% Max HP / level", "-4% Attack Interval / level",
        "+10% Gold / level", "+10% XP / level"
    };

    public static readonly string[] CapstoneText =
    {
        "10% chance to Critical Hit (x2 dmg)", "Regen 1% Max HP / sec",
        "15% chance to attack twice", "10% chance for double Gold",
        "\"Compiled successfully!\" on level up"
    };

    private static readonly int[] BaseCost = { 20, 25, 30, 35, 35 };
    private static readonly float[] CostMult = { 1.6f, 1.6f, 1.7f, 1.7f, 1.7f };
    private static readonly float[] EffectPerLevel = { 0.05f, 0.08f, 0.04f, 0.10f, 0.10f };

    private readonly int[] _levels = new int[BranchCount];

    public int GetLevel(SkillBranch b) => _levels[(int)b];
    public bool IsMaxed(SkillBranch b) => _levels[(int)b] >= MaxLevel;

    public int GetUpgradeCost(SkillBranch b)
    {
        int level = _levels[(int)b];
        if (level >= MaxLevel) return -1;
        return Mathf.RoundToInt(BaseCost[(int)b] * Mathf.Pow(CostMult[(int)b], level));
    }

    public void IncrementLevel(SkillBranch b)
    {
        int i = (int)b;
        if (_levels[i] < MaxLevel) _levels[i]++;
    }

    public float AttackMultiplier => 1f + _levels[(int)SkillBranch.CodeOptimization] * EffectPerLevel[(int)SkillBranch.CodeOptimization];
    public float HpMultiplier => 1f + _levels[(int)SkillBranch.CaffeineReserve] * EffectPerLevel[(int)SkillBranch.CaffeineReserve];
    public float IntervalMultiplier => 1f - _levels[(int)SkillBranch.KeyboardShortcuts] * EffectPerLevel[(int)SkillBranch.KeyboardShortcuts];
    public float GoldMultiplier => 1f + _levels[(int)SkillBranch.OvertimePay] * EffectPerLevel[(int)SkillBranch.OvertimePay];
    public float XpMultiplier => 1f + _levels[(int)SkillBranch.StackOverflow] * EffectPerLevel[(int)SkillBranch.StackOverflow];

    public bool CritChanceActive => IsMaxed(SkillBranch.CodeOptimization);
    public bool HpRegenActive => IsMaxed(SkillBranch.CaffeineReserve);
    public bool DoubleAttackActive => IsMaxed(SkillBranch.KeyboardShortcuts);
    public bool DoubleGoldActive => IsMaxed(SkillBranch.OvertimePay);
    public bool CompileMessageActive => IsMaxed(SkillBranch.StackOverflow);
}
