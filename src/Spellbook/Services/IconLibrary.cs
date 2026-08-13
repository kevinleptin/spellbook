namespace Spellbook.Services;

/// <summary>图标定义:Key 与 Assets/Icons/{Key}.svg 文件名一致。</summary>
public record IconDef(string Key, string DisplayName, string Category);

/// <summary>
/// 内置图标清单(100 个,魔兽主题)。
/// 顺序即选择器展示顺序(按类别分块);第一个 book 为默认图标。
/// </summary>
public static class IconLibrary
{
    public static readonly IReadOnlyList<IconDef> All = new IconDef[]
    {
        // ---- 奥术 (8) ----
        new("book", "法术书", "奥术"),
        new("scroll", "卷轴", "奥术"),
        new("arcane-missile", "奥术飞弹", "奥术"),
        new("portal", "传送门", "奥术"),
        new("rune", "符文石", "奥术"),
        new("crystal-ball", "水晶球", "奥术"),
        new("magic-circle", "魔法阵", "奥术"),
        new("arcane-star", "奥星", "奥术"),
        // ---- 武器 (20) ----
        new("sword", "利剑", "武器"),
        new("greatsword", "巨剑", "武器"),
        new("crossed-swords", "双剑交锋", "武器"),
        new("dagger", "匕首", "武器"),
        new("axe", "战斧", "武器"),
        new("double-axe", "双刃斧", "武器"),
        new("warhammer", "战锤", "武器"),
        new("mace", "钉头锤", "武器"),
        new("spear", "长矛", "武器"),
        new("halberd", "长戟", "武器"),
        new("bow", "长弓", "武器"),
        new("crossbow", "弩", "武器"),
        new("arrow", "箭矢", "武器"),
        new("staff", "法杖", "武器"),
        new("wand", "魔杖", "武器"),
        new("scythe", "镰刀", "武器"),
        new("flail", "连枷", "武器"),
        new("fist-weapon", "拳刃", "武器"),
        new("throwing-knife", "飞刀", "武器"),
        new("gun", "火枪", "武器"),
        // ---- 防具与装备 (15) ----
        new("shield-round", "圆盾", "防具与装备"),
        new("shield-tower", "塔盾", "防具与装备"),
        new("helmet", "头盔", "防具与装备"),
        new("chestplate", "胸甲", "防具与装备"),
        new("gauntlet", "护手", "防具与装备"),
        new("boots", "战靴", "防具与装备"),
        new("cloak", "披风", "防具与装备"),
        new("ring", "戒指", "防具与装备"),
        new("amulet", "项链", "防具与装备"),
        new("belt", "腰带", "防具与装备"),
        new("key", "钥匙", "防具与装备"),
        new("lock", "锁", "防具与装备"),
        new("coin", "金币", "防具与装备"),
        new("pouch", "钱袋", "防具与装备"),
        new("banner", "战旗", "防具与装备"),
        // ---- 火系 (8) ----
        new("fireball", "火球术", "火系"),
        new("flame", "烈焰", "火系"),
        new("meteor", "陨石", "火系"),
        new("explosion", "爆裂", "火系"),
        new("phoenix", "凤凰", "火系"),
        new("torch", "火把", "火系"),
        new("lava", "熔岩", "火系"),
        new("ignite", "引燃", "火系"),
        // ---- 冰系 (7) ----
        new("snowflake", "冰霜新星", "冰系"),
        new("ice-shard", "冰锥术", "冰系"),
        new("ice-shield", "寒冰护体", "冰系"),
        new("frostbolt", "寒冰箭", "冰系"),
        new("ice-ring", "冰环", "冰系"),
        new("hail", "冰雹", "冰系"),
        new("frost-breath", "霜之吐息", "冰系"),
        // ---- 雷电风暴 (6) ----
        new("lightning", "闪电束", "雷电风暴"),
        new("chain-lightning", "闪电链", "雷电风暴"),
        new("storm", "风暴之眼", "雷电风暴"),
        new("tornado", "龙卷风", "雷电风暴"),
        new("thundercloud", "雷云", "雷电风暴"),
        new("static", "静电场", "雷电风暴"),
        // ---- 神圣 (7) ----
        new("holy-light", "圣光术", "神圣"),
        new("heal-cross", "治疗术", "神圣"),
        new("angel-wings", "天使之翼", "神圣"),
        new("holy-hammer", "圣光之锤", "神圣"),
        new("halo", "光环", "神圣"),
        new("chalice", "圣杯", "神圣"),
        new("blessing-hand", "祝福之手", "神圣"),
        // ---- 暗影死灵 (8) ----
        new("skull", "骷髅", "暗影死灵"),
        new("ghost", "幽灵", "暗影死灵"),
        new("shadow-orb", "暗影之球", "暗影死灵"),
        new("cursed-eye", "诅咒之眼", "暗影死灵"),
        new("bat", "蝙蝠", "暗影死灵"),
        new("tombstone", "墓碑", "暗影死灵"),
        new("poison-flask", "剧毒药瓶", "暗影死灵"),
        new("demon-horns", "恶魔之角", "暗影死灵"),
        // ---- 自然 (8) ----
        new("leaf", "生命之叶", "自然"),
        new("tree", "世界之树", "自然"),
        new("flower", "绽放", "自然"),
        new("mushroom", "蘑菇", "自然"),
        new("vine", "缠绕藤蔓", "自然"),
        new("paw", "野性爪印", "自然"),
        new("thorns", "荆棘术", "自然"),
        new("acorn", "橡果", "自然"),
        // ---- 药剂与杂物 (13) ----
        new("potion-red", "红色药水", "药剂与杂物"),
        new("potion-blue", "蓝色药水", "药剂与杂物"),
        new("cauldron", "坩埚", "药剂与杂物"),
        new("hourglass", "沙漏", "药剂与杂物"),
        new("map", "藏宝图", "药剂与杂物"),
        new("compass", "罗盘", "药剂与杂物"),
        new("horn", "战争号角", "药剂与杂物"),
        new("drum", "战鼓", "药剂与杂物"),
        new("totem", "图腾", "药剂与杂物"),
        new("campfire", "篝火", "药剂与杂物"),
        new("meat", "烤肉", "药剂与杂物"),
        new("bread", "面包", "药剂与杂物"),
        new("chest", "宝箱", "药剂与杂物"),
    };
}
