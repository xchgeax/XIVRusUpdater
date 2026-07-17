using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Core;

public static class ComponentDefinitions
{
    public static readonly IReadOnlyList<ComponentDefinition> All =
    [
        new("enemy_npc",
            "Вражеские NPC",
            "Отключите этот компонент, если возникают трудности с поиском врагов для заданий, охоты и прочего.",
            ["BNpcName"]),

        new("place_names",
            "Названия локаций",
            "Отключите этот компонент, если испытываете трудности при поиске мест с использованием игровых wiki, гайдов и других материалов.",
            ["PlaceName", "Town"]),

        new("content_finder",
            "Названия и описания миссий",
            "Отключите этот компонент, если хотите видеть названия в Поиске миссий в оригинале.\n\nОбратите внимание, что с переводом оригинальное название миссии можно увидеть в её описании.",
            ["ContentFinderCondition", "ContentFinderConditionTransient"]),

        new("npc_names",
            "Имена дружественных NPC",
            "Отключите, если возникают трудности с поиском торговцев, NPC для ремесла, обмена и других функциональных NPC.",
            ["ENpcResident"]),

        new("actions",
            "Описания действий, навыков и статус-эффектов",
            "Отключите этот компонент, если хотите видеть указанные описания в оригинале.",
            ["Action", "ActionCategory", "ActionComboRouteTransient",
             "ActionTransient", "TraitTransient", "Status"]),

        new("achievements",
            "Достижения",
            "Отключите этот компонент, если нужны оригинальные названия достижений.\n\nОбратите внимание, что перевод описаний достижений пропадёт.",
            ["Achievement"]),

        new("titles",
            "Титулы",
            "Отключите этот компонент, если хотите видеть титулы игроков в оригинале.",
            ["Title"]),

        new("collectibles",
            "Коллекционное (см. подсказку для полного списка)",
            "Затронуто следующее:\n* Названия транспорта\n* Названия миньонов\n* Наборы рамок карточки приключенца\n* Бардинги чокобо\n* Карты Тройного Трио (перевод описаний пропадёт)\n* Записки Бозьи (перевод текста записок пропадёт)\n* Записки вариативных подземелий (перевод текста записок пропадёт)\n* Occult Records (перевод текста пропадёт)\n* Поручения (перевод описаний пропадёт)",
            ["Mount", "Companion", "CharaCardBase", "CharaCardDecoration",
             "CharaCardDesignPreset", "CharaCardHeader", "BannerBg", "BannerDecoration",
             "BannerDesignPreset", "BannerFrame", "BuddyEquip", "TripleTriadCard",
             "MYCWarResultNotebook", "VVDNotebookContents", "MKDLore", "Leve"]),

        new("emotes",
            "Эмоции",
            "Отключите этот компонент, если хотите видеть названия эмоций в оригинале.\n\nОбратите внимание, что команды эмоций в любом случае не переводятся.\nВозможно, вам будет достаточно команд вместо названий.",
            ["Emote"]),
    ];
};

public sealed record ComponentDefinition(string Id, string DisplayName, string Description, IReadOnlyList<string> Sheets, bool IsWildcard = false, string WildcardPrefix = "");
