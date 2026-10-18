using System.Collections.Generic;
using UnityEngine;

namespace AddressableLayout.Resource
{
    /// <summary>
    /// JsonUtility용 string→string 맵 (keys/values 병렬 배열).
    /// </summary>
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
            keys = new List<string>(_dictionary.Keys);
            values = new List<string>(_dictionary.Values);
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
