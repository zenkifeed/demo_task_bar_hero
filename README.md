# TBH: Task Bar Hero — Prototype

Idle RPG lấy cảm hứng từ *Task Bar Hero* trên Steam: một hero pixel tự động cày quái, lên cấp, kiếm vàng và nâng cấp sức mạnh qua Skill Tree. Prototype chạy trong Unity, dùng asset "Layer Lab 2D Minimal" cho nhân vật, quái vật và môi trường.

## Vòng lặp gameplay

1. Hero và quái vật tự động đánh nhau theo timer riêng (không cần thao tác).
2. Giết quái → nhận **Gold** + **XP**, quái tiếp theo mạnh hơn (tăng tier).
3. Đủ XP → **lên cấp**, hồi đầy máu, tăng ATK/HP.
4. Thua → mất 20% Gold, hồi đầy máu, giảm 1 tier quái.
5. Dùng **Gold** mở panel **Skills** để nâng cấp Skill Tree, tăng sức mạnh vĩnh viễn cho hero.

## Hệ thống

### Combat (`IdleCombatManager.cs`)

- Hero tấn công mỗi `attackInterval` giây (mặc định 1s, giảm dần nhờ Skill Tree), quái tấn công mỗi `enemyAttackInterval` giây (1.3s).
- Sát thương/HP hiệu dụng luôn tính qua các multiplier của Skill Tree (`EffectiveAttack`, `EffectiveMaxHp`, `EffectiveAttackInterval`) — chỉ số gốc (`attack`, `maxHp`, `attackInterval`) không đổi, multiplier nhân thêm lên trên.
- Enemy tier tăng dần theo cấp số nhân đơn giản: `maxHp = 15 + tier*6`, `attack = 2 + tier*0.8`, `goldReward = 3 + tier`, `xpReward = 5 + tier*2`.
- Enemy được chọn theo vòng lặp tên (10 quái/nhóm) lồng với 10 prefab quái vật (Layer Lab EnemyMonster 2) theo index `tier % 10`.

### Danh sách quái (theo thứ tự tier)

| Tên | Prefab |
|---|---|
| Bug | Spider_Red |
| Deadline | Skeleton_Warrior |
| Merge Conflict | Golem_Iron |
| Popup Ad | Fly_Bigeye |
| Spam Email | Bee_Hornet |
| 404 Error | Ghost_Mask |
| Slow Wi-Fi | Bat_Cave |
| Meeting Invite | Mantis_Green |
| Cache Miss | Scorpion_Purple |
| Memory Leak | Plant_Red |

Sau 10 quái, vòng lặp lại với hậu tố `x2`, `x3`... (chỉ số vẫn tăng theo tier tuyệt đối).

### Leveling

- `xpToNextLevel = 20 + level*15`, tăng dần mỗi lần lên cấp.
- Mỗi cấp: `maxHp += 8`, `attack += 1.5`, hồi đầy máu.

### Skill Tree (`SkillTreeManager.cs`)

5 nhánh độc lập, mỗi nhánh 8 cấp, chi phí tăng theo cấp số nhân (`cost = baseCost × mult^(level-1)`), trả bằng Gold, hiệu ứng nhân dồn:

| Nhánh | Hiệu ứng/cấp | Tối đa (Lv8) | Chi phí | Capstone (Lv8) |
|---|---|---|---|---|
| Code Optimization | +5% ATK | +40% ATK | base 20, ×1.6 | 10% chí mạng x2 dmg |
| Caffeine Reserve | +8% Max HP | +64% HP | base 25, ×1.6 | Hồi 1% Max HP/giây |
| Keyboard Shortcuts | −4% attack interval | −28% interval | base 30, ×1.7 | 15% đánh 2 lần liên tiếp |
| Overtime Pay | +10% Gold | +80% Gold | base 35, ×1.7 | 10% x2 Gold khi giết quái |
| Stack Overflow | +10% XP | +80% XP | base 35, ×1.7 | Log "Compiled successfully!" khi lên cấp |

Tổng gold để max cả 5 nhánh: ~13,000g. Nâng cấp qua UI (nút **Skills** góc trên-phải màn hình) hoặc gọi thẳng `IdleCombatManager.UpgradeSkill(int branchIndex)` (0=Code Optimization ... 4=Stack Overflow).

## Cấu trúc project

```
Assets/
  Scenes/TaskBarHero.unity        # scene chính
  Scripts/TaskBarHero/
    IdleCombatManager.cs          # game loop, combat, leveling, UI refresh
    SkillTreeManager.cs           # level tracking + multiplier cho skill tree
  Layer Lab/
    2D Minimal-CharacterMaker/    # rig hero + hệ thống ghép trang bị (PartsManager)
    2D Minimal-EnemyMonster/      # 10 prefab quái vật (EnemyMonster 2 pack)
    2D Minimal-Environment/       # tile nền + prop trang trí
```

### Hierarchy trong scene `TaskBarHero`

- `Canvas/TaskBarPanel` — HUD: tên/level hero, thanh HP/XP, tên/HP quái, gold, thời gian chơi, log chiến đấu.
- `Canvas/SkillsButton`, `Canvas/SkillTreePanel` — UI Skill Tree (ẩn mặc định, bấm Skills để mở).
- `GameManager` — chứa `IdleCombatManager` + `SkillTreeManager`.
- `HeroDisplay` — rig hero (Layer Lab Character Maker, đang trang bị Bow).
- `EnemySpawnPoint` — vị trí spawn quái.
- `Background` / `BackgroundProps` — nền rừng tiled + cây/bụi/đá trang trí.

## Chạy thử

1. Mở project bằng Unity 6000.2.6f2 (hoặc mới hơn).
2. Mở scene `Assets/Scenes/TaskBarHero.unity`.
3. Nhấn Play — hero tự động chiến đấu, theo dõi HUD góc dưới; bấm **Skills** để nâng cấp khi có đủ Gold.

## Giới hạn hiện tại (prototype)

- Chưa có save/load persistent giữa các phiên chơi (state reset khi thoát Play mode).
- Chưa có hệ thống item/pet như bản gốc Task Bar Hero — chỉ mới có combat + leveling + skill tree.
- Chưa build thành ứng dụng chạy nền taskbar thật (đang chạy trong cửa sổ Game view của Unity).
