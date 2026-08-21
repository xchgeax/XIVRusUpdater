using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace XIVRusUpdater.Core.Components;

public static class TranslationEngines
{
    public static IReadOnlyList<TranslationEngine> All { get; } =
        Assembly.GetExecutingAssembly().DefinedTypes
            .Select(t => t.GetCustomAttribute<TranslationEngineDefinition>())
            .Where(a => a is not null)
            .Cast<TranslationEngineDefinition>()
            .Select(a => new TranslationEngine(a))
            .ToArray();

    private static IReadOnlyDictionary<string, TranslationEngine> ById { get; } =
        All.ToDictionary(x => x.Id);

    public static TranslationEngine? Get(string id) =>
        ById.GetValueOrDefault(id);
}

public sealed class TranslationEngine
{
    public TranslationEngineDefinition Definition { get; }

    public string Id => Definition.Id;
    public string ModName => Definition.ModName;
    public string DisplayName => Definition.DisplayName;
    public string ApiUrl => Definition.ApiUrl;

    public DirectoryInfo? ModPath =>
        Plugin.PenumbraApi.GetModPath(ModName);

    public string Version =>
        Plugin.PenumbraApi.GetModVersion(ModName) ?? "0.0.0";

    public TranslationEngine(TranslationEngineDefinition definition)
    {
        Definition = definition;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class TranslationEngineDefinition : Attribute
{
    public string Id { get; }
    public string ModName { get; }
    public string DisplayName { get; }
    public string ApiUrl { get; }

    public TranslationEngineDefinition(string id, string modName, string dispayName, string api)
    {
        Id = id;
        ModName = modName;
        DisplayName = dispayName;
        ApiUrl = api;
    }
}

[TranslationEngineDefinition("XIVRusJapanese", "XIV Rus", "XIVRus — Japanese", "https://update.xivrus.ru/api/jp/")]
public sealed class XIVRusJapaneseEngine;

[TranslationEngineDefinition("XIVRusEnglish", "XIV Rus", "XIVRus — English", "https://update.xivrus.ru/api")]
public sealed class XIVRusEnglishEngine;
