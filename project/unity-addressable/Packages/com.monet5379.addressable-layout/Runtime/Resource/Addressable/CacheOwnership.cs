using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace AddressableLayout.Resource
{
    internal sealed class CacheOwnership
    {
        public const string Direct = "\0direct";

        private readonly Dictionary<string, Entry> _entries = new();
        private readonly Dictionary<string, string> _keyToPrimary = new();
        private readonly Dictionary<string, HashSet<string>> _ownerToPrimaries = new();

        public void Add(string primaryKey, Object asset, string owner)
        {
            if (string.IsNullOrEmpty(primaryKey) || asset == null || string.IsNullOrEmpty(owner))
            {
                return;
            }

            if (!_entries.TryGetValue(primaryKey, out Entry entry))
            {
                entry = new Entry();
                _entries.Add(primaryKey, entry);
            }

            entry.Asset = asset;
            entry.Owners.Add(owner);
            _keyToPrimary[primaryKey] = primaryKey;
            AddOwnerIndex(owner, primaryKey);
        }

        public void AddAlias(string primaryKey, string alias)
        {
            if (string.IsNullOrEmpty(primaryKey) || string.IsNullOrEmpty(alias) || alias == primaryKey)
            {
                return;
            }

            if (!_entries.TryGetValue(primaryKey, out Entry entry))
            {
                return;
            }

            if (_keyToPrimary.TryGetValue(alias, out string existingPrimary) && existingPrimary != primaryKey
                && _entries.TryGetValue(existingPrimary, out Entry previousEntry))
            {
                previousEntry.Aliases.Remove(alias);
            }

            _keyToPrimary[alias] = primaryKey;
            entry.Aliases.Add(alias);
        }

        public void AddRequestAlias(string requestKey, string primaryKey)
        {
            if (string.IsNullOrEmpty(requestKey)
                || string.Equals(requestKey, primaryKey, System.StringComparison.Ordinal))
            {
                return;
            }

            AddAlias(primaryKey, requestKey);
        }

        public void AddLocationAliasForCache(IResourceLocation location, string primaryKey)
        {
            if (location == null)
            {
                return;
            }

            string internalId = location.InternalId;
            if (string.IsNullOrEmpty(internalId)
                || string.Equals(internalId, primaryKey, System.StringComparison.Ordinal))
            {
                return;
            }

            AddAlias(primaryKey, internalId);
        }

        public bool TryGet<T>(string key, out T asset) where T : Object
        {
            asset = null;
            if (!TryResolve(key, out string primaryKey) || !_entries.TryGetValue(primaryKey, out Entry entry))
            {
                return false;
            }

            if (entry.Asset is T typed)
            {
                asset = typed;
                return true;
            }

            return false;
        }

        public bool Contains(string key)
        {
            return TryResolve(key, out string primaryKey) && _entries.ContainsKey(primaryKey);
        }

        public bool TryResolve(string key, out string primaryKey)
        {
            primaryKey = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return _keyToPrimary.TryGetValue(key, out primaryKey);
        }

        public bool HasOwner(string primaryKey, string owner)
        {
            return !string.IsNullOrEmpty(primaryKey)
                && !string.IsNullOrEmpty(owner)
                && _entries.TryGetValue(primaryKey, out Entry entry)
                && entry.Owners.Contains(owner);
        }

        public bool Release(string primaryKey, string owner)
        {
            if (string.IsNullOrEmpty(primaryKey) || string.IsNullOrEmpty(owner))
            {
                return false;
            }

            if (!_entries.TryGetValue(primaryKey, out Entry entry) || !entry.Owners.Remove(owner))
            {
                return false;
            }

            RemoveOwnerIndex(owner, primaryKey);
            if (entry.Owners.Count == 0)
            {
                Evict(primaryKey);
            }

            return true;
        }

        public void ReleaseOwner(string owner)
        {
            if (string.IsNullOrEmpty(owner)
                || !_ownerToPrimaries.TryGetValue(owner, out HashSet<string> primaries))
            {
                return;
            }

            var snapshot = new List<string>(primaries);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Release(snapshot[i], owner);
            }
        }

        public void Clear()
        {
            _entries.Clear();
            _keyToPrimary.Clear();
            _ownerToPrimaries.Clear();
        }

        private void AddOwnerIndex(string owner, string primaryKey)
        {
            if (!_ownerToPrimaries.TryGetValue(owner, out HashSet<string> primaries))
            {
                primaries = new HashSet<string>();
                _ownerToPrimaries.Add(owner, primaries);
            }

            primaries.Add(primaryKey);
        }

        private void RemoveOwnerIndex(string owner, string primaryKey)
        {
            if (!_ownerToPrimaries.TryGetValue(owner, out HashSet<string> primaries))
            {
                return;
            }

            primaries.Remove(primaryKey);
            if (primaries.Count == 0)
            {
                _ownerToPrimaries.Remove(owner);
            }
        }

        private void Evict(string primaryKey)
        {
            if (!_entries.TryGetValue(primaryKey, out Entry entry))
            {
                return;
            }

            _entries.Remove(primaryKey);
            _keyToPrimary.Remove(primaryKey);
            foreach (string alias in entry.Aliases)
            {
                _keyToPrimary.Remove(alias);
            }

            foreach (string owner in entry.Owners)
            {
                RemoveOwnerIndex(owner, primaryKey);
            }
        }

        private sealed class Entry
        {
            public Object Asset;
            public readonly HashSet<string> Owners = new();
            public readonly HashSet<string> Aliases = new();
        }
    }
}
