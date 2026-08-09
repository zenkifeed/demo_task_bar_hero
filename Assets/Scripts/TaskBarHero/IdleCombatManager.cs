using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LayerLab.ArtMakerUnity;

// Idle auto-battler: hero and enemy trade blows on independent timers.
// Killing an enemy grants gold/XP and spawns a tougher one; leveling
// scales hero stats and heals to full.
public class IdleCombatManager : MonoBehaviour
{
    [Header("Hero")]
    public string heroName = "Warrior";
    public int level = 1;
    public float xp = 0f;
    public float xpToNextLevel = 35f;
    public float maxHp = 50f;
    public float currentHp = 50f;
    public float attack = 5f;
    public float attackInterval = 1f;
    public int gold = 0;

    [Header("Enemy Tuning")]
    public float enemyAttackInterval = 1.3f;

    [Header("Enemy Visual")]
    public GameObject[] enemyPrefabs;
    public Transform enemySpawnPoint;
    private GameObject _enemyVisual;
    private Animator _enemyAnimator;
    private bool _enemyDefeated;
    private Coroutine _enemyReturnToIdleRoutine;

    [Header("Hero Visual")]
    public PartsManager heroParts;
    private const string AnimIdle = "Idle";
    private const string AnimAttack = "Attack";
    private const string AnimVictory = "Victory";
    private const string AnimDefeat = "Defeat";
    private Coroutine returnToIdleRoutine;

    [Header("Floating Combat Text")]
    public Canvas mainCanvas;

    [Header("Skill Tree")]
    public SkillTreeManager skillTree;
    public GameObject skillTreePanel;
    public Text[] skillNameTexts = new Text[SkillTreeManager.BranchCount];
    public Text[] skillLevelTexts = new Text[SkillTreeManager.BranchCount];
    public Text[] skillCostTexts = new Text[SkillTreeManager.BranchCount];
    public Button[] skillUpgradeButtons = new Button[SkillTreeManager.BranchCount];

    [Header("UI References")]
    public Text heroNameLevelText;
    public Slider heroHpSlider;
    public Text heroHpText;
    public Slider heroXpSlider;
    public Text heroXpText;
    public Text enemyNameText;
    public Slider enemyHpSlider;
    public Text enemyHpText;
    public Text goldText;
    public Text sessionTimeText;
    public Text logText;

    private static readonly string[] EnemyNames =
    {
        "Bug", "Deadline", "Merge Conflict", "Popup Ad", "Spam Email",
        "404 Error", "Slow Wi-Fi", "Meeting Invite", "Cache Miss", "Memory Leak"
    };

    private const int MaxLogLines = 5;
    private readonly Queue<string> _logLines = new Queue<string>();

    private int _enemyTier;
    private string _enemyName;
    private float _enemyMaxHp;
    private float _enemyCurrentHp;
    private float _enemyAttack;
    private float _enemyGoldReward;
    private float _enemyXpReward;

    private float _heroAttackTimer;
    private float _enemyAttackTimer;

    private void Start()
    {
        SpawnEnemy();
        RefreshUI();
    }

    private void Update()
    {
        HandleCombat(Time.deltaTime);
        RefreshUI();
    }

    private void SpawnFloatingText(Vector3 worldPos, string text, Color color, int fontSize = 18)
    {
        if (mainCanvas == null) return;
        var camera = Camera.main;
        if (camera == null) return;

        var canvasRect = mainCanvas.transform as RectTransform;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPos);
        var eventCamera = mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out var localPoint))
        {
            FloatingCombatText.Spawn(canvasRect, localPoint, text, color, fontSize);
        }
    }

    private float EffectiveAttack => attack * (skillTree != null ? skillTree.AttackMultiplier : 1f);
    private float EffectiveMaxHp => maxHp * (skillTree != null ? skillTree.HpMultiplier : 1f);
    private float EffectiveAttackInterval => Mathf.Max(0.2f, attackInterval * (skillTree != null ? skillTree.IntervalMultiplier : 1f));

    private void HandleCombat(float dt)
    {
        if (skillTree != null && skillTree.HpRegenActive && currentHp < EffectiveMaxHp)
        {
            currentHp = Mathf.Min(EffectiveMaxHp, currentHp + EffectiveMaxHp * 0.01f * dt);
        }

        if (_enemyDefeated) return;

        _heroAttackTimer -= dt;
        if (_heroAttackTimer <= 0f)
        {
            _heroAttackTimer += EffectiveAttackInterval;
            DealHeroDamage();
            if (_enemyCurrentHp <= 0f)
            {
                OnEnemyDefeated();
                return;
            }
            PlayEnemyAnim("Hit", 0.35f);

            if (skillTree != null && skillTree.DoubleAttackActive && Random.value < 0.15f)
            {
                DealHeroDamage(bonusHit: true);
                if (_enemyCurrentHp <= 0f)
                {
                    OnEnemyDefeated();
                    return;
                }
                PlayEnemyAnim("Hit", 0.35f);
            }
        }

        _enemyAttackTimer -= dt;
        if (_enemyAttackTimer <= 0f)
        {
            _enemyAttackTimer += enemyAttackInterval;
            currentHp -= _enemyAttack;
            Log($"{_enemyName} hits {heroName} for {_enemyAttack:0} dmg.");
            PlayEnemyAnim("Attack", 0.5f);
            if (heroParts != null) SpawnFloatingText(heroParts.transform.position + Vector3.up * 1.3f, $"-{_enemyAttack:0}", new Color(1f, 0.25f, 0.25f));
            if (currentHp <= 0f)
            {
                OnHeroDefeated();
            }
        }
    }

    private void DealHeroDamage(bool bonusHit = false)
    {
        float dmg = EffectiveAttack;
        bool isCrit = skillTree != null && skillTree.CritChanceActive && Random.value < 0.10f;
        if (isCrit) dmg *= 2f;
        _enemyCurrentHp -= dmg;
        string suffix = isCrit ? " (CRIT!)" : bonusHit ? " (bonus hit!)" : "";
        Log($"{heroName} hits {_enemyName} for {dmg:0} dmg{suffix}.");
        PlayHeroAnim(AnimAttack, 0.5f);

        if (_enemyVisual != null)
        {
            Color color = isCrit ? new Color(1f, 0.55f, 0.1f) : Color.white;
            SpawnFloatingText(_enemyVisual.transform.position + Vector3.up * 0.6f, isCrit ? $"-{dmg:0}!" : $"-{dmg:0}", color, isCrit ? 40 : 30);
        }
    }

    private void OnEnemyDefeated()
    {
        int goldGain = Mathf.RoundToInt(_enemyGoldReward * (skillTree != null ? skillTree.GoldMultiplier : 1f));
        bool doubleGold = skillTree != null && skillTree.DoubleGoldActive && Random.value < 0.10f;
        if (doubleGold) goldGain *= 2;
        gold += goldGain;

        float xpGain = _enemyXpReward * (skillTree != null ? skillTree.XpMultiplier : 1f);
        GainXp(xpGain);

        Log($"{_enemyName} defeated! +{goldGain}g{(doubleGold ? " (DOUBLE!)" : "")} +{Mathf.RoundToInt(xpGain)}xp");
        if (_enemyVisual != null)
        {
            string goldSuffix = doubleGold ? "!!" : "";
            SpawnFloatingText(_enemyVisual.transform.position + Vector3.up * 0.3f, $"+{goldGain}g{goldSuffix}", new Color(1f, 0.85f, 0.3f));
        }
        PlayHeroAnim(AnimVictory, 0.6f);
        if (_enemyAnimator != null) _enemyAnimator.Play("Dead");
        _enemyTier++;
        _enemyDefeated = true;
        StartCoroutine(DeferredSpawn(0.6f));
    }

    private IEnumerator DeferredSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        _enemyDefeated = false;
        SpawnEnemy();
    }

    private void OnHeroDefeated()
    {
        int lost = Mathf.RoundToInt(gold * 0.2f);
        gold -= lost;
        currentHp = EffectiveMaxHp;
        _enemyTier = Mathf.Max(0, _enemyTier - 1);
        Log($"{heroName} was defeated! Lost {lost}g. Patched up and back at it.");
        PlayHeroAnim(AnimDefeat, 0.9f);
    }

    private void PlayHeroAnim(string animName, float holdSeconds)
    {
        if (heroParts == null) return;
        heroParts.PlayAnimation(animName);
        if (returnToIdleRoutine != null) StopCoroutine(returnToIdleRoutine);
        returnToIdleRoutine = StartCoroutine(ReturnToIdleAfter(holdSeconds));
    }

    private IEnumerator ReturnToIdleAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        heroParts.PlayAnimation(AnimIdle);
        returnToIdleRoutine = null;
    }

    private void PlayEnemyAnim(string animName, float holdSeconds)
    {
        if (_enemyAnimator == null) return;
        _enemyAnimator.Play(animName);
        if (_enemyReturnToIdleRoutine != null) StopCoroutine(_enemyReturnToIdleRoutine);
        _enemyReturnToIdleRoutine = StartCoroutine(EnemyReturnToIdleAfter(holdSeconds));
    }

    private IEnumerator EnemyReturnToIdleAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (_enemyAnimator != null) _enemyAnimator.Play(AnimIdle);
        _enemyReturnToIdleRoutine = null;
    }

    private void GainXp(float amount)
    {
        xp += amount;
        while (xp >= xpToNextLevel)
        {
            xp -= xpToNextLevel;
            level++;
            maxHp += 8f;
            attack += 1.5f;
            currentHp = EffectiveMaxHp;
            xpToNextLevel = 20f + level * 15f;
            string compileMsg = skillTree != null && skillTree.CompileMessageActive ? " \"Compiled successfully!\"" : "";
            Log($"Level up! {heroName} is now level {level}.{compileMsg}");
        }
    }

    private void SpawnEnemy()
    {
        int cycle = _enemyTier / EnemyNames.Length + 1;
        string baseName = EnemyNames[_enemyTier % EnemyNames.Length];
        _enemyName = cycle > 1 ? $"{baseName} x{cycle}" : baseName;
        _enemyMaxHp = 15f + _enemyTier * 6f;
        _enemyCurrentHp = _enemyMaxHp;
        _enemyAttack = 2f + _enemyTier * 0.8f;
        _enemyGoldReward = 3f + _enemyTier * 1f;
        _enemyXpReward = 5f + _enemyTier * 2f;
        _enemyAttackTimer = enemyAttackInterval * 0.5f;

        if (_enemyVisual != null) Destroy(_enemyVisual);
        _enemyAnimator = null;
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            GameObject prefab = enemyPrefabs[_enemyTier % enemyPrefabs.Length];
            if (prefab != null)
            {
                Vector3 pos = enemySpawnPoint != null ? enemySpawnPoint.position : Vector3.zero;
                _enemyVisual = Instantiate(prefab, pos, Quaternion.identity);
                _enemyAnimator = _enemyVisual.GetComponentInChildren<Animator>();
                if (_enemyAnimator != null) _enemyAnimator.Play(AnimIdle);
            }
        }
    }

    private void Log(string line)
    {
        _logLines.Enqueue(line);
        while (_logLines.Count > MaxLogLines) _logLines.Dequeue();
        if (logText != null) logText.text = string.Join("\n", _logLines.ToArray());
    }

    public void ToggleSkillTreePanel()
    {
        if (skillTreePanel != null) skillTreePanel.SetActive(!skillTreePanel.activeSelf);
    }

    public void UpgradeSkill(int branchIndex)
    {
        if (skillTree == null) return;
        var branch = (SkillBranch)branchIndex;
        int cost = skillTree.GetUpgradeCost(branch);
        if (cost < 0 || gold < cost) return;
        gold -= cost;
        skillTree.IncrementLevel(branch);
        Log($"Upgraded {SkillTreeManager.DisplayNames[branchIndex]} to Lv.{skillTree.GetLevel(branch)}!");
    }

    private void RefreshSkillTreeUI()
    {
        if (skillTree == null) return;
        for (int i = 0; i < SkillTreeManager.BranchCount; i++)
        {
            var branch = (SkillBranch)i;
            int level = skillTree.GetLevel(branch);
            int cost = skillTree.GetUpgradeCost(branch);
            bool maxed = cost < 0;

            if (i < skillNameTexts.Length && skillNameTexts[i] != null)
                skillNameTexts[i].text = $"{SkillTreeManager.DisplayNames[i]}\n{SkillTreeManager.FlavorText[i]}";
            if (i < skillLevelTexts.Length && skillLevelTexts[i] != null)
                skillLevelTexts[i].text = maxed ? $"Lv.{level}/{SkillTreeManager.MaxLevel} (MAX)" : $"Lv.{level}/{SkillTreeManager.MaxLevel}";
            if (i < skillCostTexts.Length && skillCostTexts[i] != null)
                skillCostTexts[i].text = maxed ? SkillTreeManager.CapstoneText[i] : $"Cost: {cost}g";
            if (i < skillUpgradeButtons.Length && skillUpgradeButtons[i] != null)
                skillUpgradeButtons[i].interactable = !maxed && gold >= cost;
        }
    }

    private void RefreshUI()
    {
        if (heroNameLevelText != null) heroNameLevelText.text = $"{heroName} — Lv.{level}";
        if (heroHpSlider != null) { heroHpSlider.maxValue = EffectiveMaxHp; heroHpSlider.value = currentHp; }
        if (heroHpText != null) heroHpText.text = $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(EffectiveMaxHp)}";
        if (heroXpSlider != null) { heroXpSlider.maxValue = xpToNextLevel; heroXpSlider.value = xp; }
        if (heroXpText != null) heroXpText.text = $"{Mathf.FloorToInt(xp)}/{Mathf.CeilToInt(xpToNextLevel)} XP";
        if (enemyNameText != null) enemyNameText.text = _enemyName;
        if (enemyHpSlider != null) { enemyHpSlider.maxValue = _enemyMaxHp; enemyHpSlider.value = Mathf.Max(0f, _enemyCurrentHp); }
        if (enemyHpText != null) enemyHpText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, _enemyCurrentHp))}/{Mathf.CeilToInt(_enemyMaxHp)}";
        if (goldText != null) goldText.text = $"Gold: {gold}";
        RefreshSkillTreeUI();
        if (sessionTimeText != null)
        {
            float t = Time.timeSinceLevelLoad;
            sessionTimeText.text = $"Session {(int)(t / 60):00}:{(int)(t % 60):00}";
        }
    }
}
