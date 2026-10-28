using System.Collections;
using UnityEngine;

namespace AddressableLayout.Resource
{
    internal sealed class AddressableLabelGroupRunner : MonoBehaviour
    {
        private static AddressableLabelGroupRunner _instance;

        internal static Coroutine Run(IEnumerator routine)
        {
            if (_instance == null)
            {
                GameObject host = new GameObject("[AddressableLabelGroupRunner]");
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<AddressableLabelGroupRunner>();
            }

            return _instance.StartCoroutine(routine);
        }
    }
}
