using System.Collections.Generic;
using UnityEngine;

namespace AddressableLayout.Resource
{
    [System.Serializable]
    public sealed class StringKeyedMap : ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<string> keys;

        [SerializeField]
        private List<string> values;

        private Dictionary<string, string> _dictionary;

        public StringKeyedMap()
        {
            _dictionary = new Dictionary<string, string>();
        }

        public StringKeyedMap(Dictionary<string, string> dictionary)
        {
            _dictionary = dictionary ?? new Dictionary<string, string>();
        }

        public Dictionary<string, string> ToDictionary()
        {
            return _dictionary;
        }

        public void OnBeforeSerialize()
        {
            // 내용이 같다면 같은 바이트가 되도록 정렬
            var sortedKeys = new List<string>(_dictionary.Keys);
            sortedKeys.Sort(System.StringComparer.Ordinal);
            keys = sortedKeys;
            values = new List<string>(sortedKeys.Count);
            foreach (string key in sortedKeys)
            {
                values.Add(_dictionary[key]);
            }
        }

        public void OnAfterDeserialize()
        {
            int count = keys == null || values == null
                ? 0
                : Mathf.Min(keys.Count, values.Count);

            _dictionary = new Dictionary<string, string>(count);
            for (int i = 0; i < count; i++)
            {
                if (string.IsNullOrEmpty(keys[i]) || _dictionary.ContainsKey(keys[i]))
                {
                    continue;
                }

                _dictionary.Add(keys[i], values[i]);
            }
        }
    }
}
