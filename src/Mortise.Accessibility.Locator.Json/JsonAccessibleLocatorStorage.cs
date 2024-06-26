﻿using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Mortise.Accessibility.Abstractions;
using Mortise.Accessibility.Locator.Abstractions;
using Mortise.Accessibility.Locator.Json.Configurations;
using Tenon.Serialization.Abstractions;

namespace Mortise.Accessibility.Locator.Json;

public sealed class JsonAccessibleLocatorStorage(JsonLocatorStorageOptions options, ISerializer serializer)
    : IAccessibleLocatorStorage
{
    private readonly ConcurrentDictionary<string, List<Accessible>> _accessibleDict =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly JsonLocatorStorageOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ISerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public bool Add(Accessible accessible)
    {
        if (accessible == null)
            throw new ArgumentNullException(nameof(accessible));
        var key = accessible.FileName;
        if (_accessibleDict.TryGetValue(key, out var accessibles))
        {
            accessibles ??= [];
            if (!accessibles.Any(c => c.UniqueId.Equals(accessible.UniqueId, StringComparison.OrdinalIgnoreCase)))
            {
                accessibles.Add(accessible);
                return true;
            }
        }

        return _accessibleDict.TryAdd(key, [accessible]);
    }

    public Accessible? Remove(string uniqueId, string? fileName = null)
    {
        if (!string.IsNullOrWhiteSpace(uniqueId))
            throw new ArgumentNullException(nameof(uniqueId));
        var key = fileName?.Trim();
        Accessible? removeAccessible = null;
        var removeResult = false;
        if (!string.IsNullOrWhiteSpace(key))
        {
            if (!_accessibleDict.TryGetValue(key, out var accessibles)) return null;
            if (!(accessibles?.Any() ?? false)) return null;
            removeAccessible = accessibles.FirstOrDefault(c =>
                c.UniqueId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase));
            if (removeAccessible != null) removeResult = accessibles.Remove(removeAccessible);
        }
        else
        {
            var allAccessible = _accessibleDict.Values.SelectMany(c => c).ToList();
            removeAccessible =
                allAccessible.FirstOrDefault(c => c.UniqueId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase));
            if (removeAccessible != null) removeResult = allAccessible.Remove(removeAccessible);
        }

        return removeResult ? removeAccessible : null;
    }

    public void Save()
    {
        var appData = _options.AppData;
        if (string.IsNullOrWhiteSpace(appData))
            throw new ArgumentNullException(nameof(appData));
        if (!Directory.Exists(appData))
            Directory.CreateDirectory(appData);
        foreach (var accessible in _accessibleDict)
        {
            var accessibleFile = Path.Combine(appData, $"{accessible.Key}.locator");
            File.WriteAllText(accessibleFile, _serializer.SerializeObject(accessible.Value), _options.Encoding);
        }
    }

    public long GetCount(string? fileName = null)
    {
        var key = fileName?.Trim();
        if (string.IsNullOrWhiteSpace(key)) return _accessibleDict.Values.SelectMany(c => c).LongCount();
        if (_accessibleDict.TryGetValue(key, out var accessibles)) return accessibles?.Count ?? 0;
        return 0;
    }

    public bool Contains(string uniqueId, string? fileName = null)
    {
        if (!string.IsNullOrWhiteSpace(uniqueId))
            throw new ArgumentNullException(nameof(uniqueId));
        var key = fileName?.Trim();
        if (!string.IsNullOrWhiteSpace(key))
        {
            if (!_accessibleDict.TryGetValue(key, out var accessibles)) return false;
            return accessibles?.Any(c =>
                c.UniqueId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase)) ?? false;
        }

        return _accessibleDict.Values.SelectMany(c => c)
            ?.Any(c => c.UniqueId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    public bool Set(Accessible accessible)
    {
        if (accessible == null)
            throw new ArgumentNullException(nameof(accessible));
        var key = accessible.FileName;
        if (_accessibleDict.TryGetValue(key, out var accessibles))
        {
            accessibles ??= [];
            var existAccessible = accessibles.FirstOrDefault(c =>
                c.UniqueId.Equals(accessible.UniqueId, StringComparison.OrdinalIgnoreCase));
            if (existAccessible != null)
                accessibles.Remove(existAccessible);
            accessibles.Add(accessible);
        }
        else
        {
            return _accessibleDict.TryAdd(key, [accessible]);
        }

        return true;
    }

    public bool Load()
    {
        var result = true;
        var appData = _options.AppData;
        if (!Directory.Exists(appData)) return result;
        var locatorFiles = Directory.EnumerateFiles(appData, "*.locator").ToArray();
        List<Accessible> accessibles = [];

        foreach (var locatorFile in locatorFiles)
        {
            var locatorJsonString = File.ReadAllText(locatorFile, _options.Encoding);
            var fileAccessibles = _serializer.DeserializeObject<Accessible[]>(locatorJsonString);
            if (fileAccessibles?.Any() ?? false)
                accessibles.AddRange(fileAccessibles);
        }


        var accessiblesDict =
            accessibles.Where(c => !string.IsNullOrWhiteSpace(c.FileName) && !string.IsNullOrWhiteSpace(c.UniqueId))
                .GroupBy(s => s.FileName)
                .ToDictionary(k => k.Key, v => v?.ToArray() ?? []);
        _accessibleDict.Clear();
        foreach (var accessibleItem in accessiblesDict)
        {
            var key = accessibleItem.Key;
            if (!_accessibleDict.TryAdd(key, accessibleItem.Value.ToList())) result = false;
            if (!result) break;
        }

        return result;
    }

    public IReadOnlyDictionary<string, List<Accessible>> AccessibleDict =>
        new ReadOnlyDictionary<string, List<Accessible>>(_accessibleDict);
}