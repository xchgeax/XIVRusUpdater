using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace XIVRusUpdater.Core.Components;

// Insipired by Infiziert90's Submarine Tracker component based commands.
public static class TranslationComponents
{
    public static IReadOnlyList<TranslationComponent> All { get; } =
        Assembly.GetExecutingAssembly().DefinedTypes.Select(t => t.GetCustomAttribute<TranslationComponent>()).Where(a => a is not null).Cast<TranslationComponent>().ToArray();
};

[AttributeUsage(AttributeTargets.Class)]
public sealed class TranslationComponent : Attribute
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public IReadOnlyList<string> Sheets { get; }
    public bool IsWildcard { get; } = false;
    public string WildcardPrefix { get; } = "";

    public TranslationComponent(string id, string displayName, string description, string[] sheets, bool isWildcard = false, string wildcardPrefix = "")
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Sheets = sheets;
        IsWildcard = isWildcard;
        WildcardPrefix = wildcardPrefix;
    }
}

[TranslationComponent("enemy_npc", "Enemy NPCs", "Disable if translated enemy names make it difficult to follow guides or complete quests.", ["BNpcName"])]
public sealed class EnemyNpcComponent;

[TranslationComponent("place_names", "Place Names", "Disable if translated location names make it difficult to use wikis, maps, or guides.", ["PlaceName", "Town"])]
public sealed class PlaceNamesComponent;

[TranslationComponent("content_finder", "Duty Names and Descriptions", "Disable if you prefer to see original duty names in Duty Finder. The original name will still be shown in the description when translations are enabled.", ["ContentFinderCondition", "ContentFinderConditionTransient"])]
public sealed class ContentFinderComponent;

[TranslationComponent("npc_names", "Friendly NPC Names", "Disable if translated NPC names make it difficult to find vendors, quest NPCs, or other service NPCs.", ["ENpcResident"])]
public sealed class FriendlyNpcComponent;

[TranslationComponent("actions", "Actions, Traits and Statuses", "Disable if you prefer action, trait, and status descriptions in the original language.", [
    "Action",
    "ActionCategory",
    "ActionComboRouteTransient",
    "ActionTransient",
    "TraitTransient",
    "Status"
])]
public sealed class ActionsComponent;

[TranslationComponent("achievements", "Achievements", "Disable if you prefer achievement names in the original language. Achievement descriptions will also remain untranslated.", ["Achievement"])]
public sealed class AchievementsComponent;

[TranslationComponent("titles", "Titles", "Disable if you prefer player titles in the original language.", ["Title"])]
public sealed class TitlesComponent;

[TranslationComponent("collectibles", "Collectibles", "Includes mounts, minions, Adventurer Plate customization, chocobo barding, Triple Triad cards, Bozjan notes, Variant Dungeon records, Occult Records, and leves.", [
    "Mount",
    "Companion",
    "CharaCardBase",
    "CharaCardDecoration",
    "CharaCardDesignPreset",
    "CharaCardHeader",
    "BannerBg",
    "BannerDecoration",
    "BannerDesignPreset",
    "BannerFrame",
    "BuddyEquip",
    "TripleTriadCard",
    "MYCWarResultNotebook",
    "VVDNotebookContents",
    "MKDLore",
    "Leve"
])]
public sealed class CollectiblesComponent;

[TranslationComponent("emotes", "Emotes", "Disable if you prefer emote names in the original language. Emote commands are never translated.", ["Emote"])]
public sealed class EmotesComponent;
