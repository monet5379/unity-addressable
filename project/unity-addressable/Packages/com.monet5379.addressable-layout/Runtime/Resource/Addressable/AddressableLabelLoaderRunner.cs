using System.Collections;
using UnityEngine;

namespace AddressableLayout.Resource
{
    [AddComponentMenu("")]
    internal sealed class AddressableLabelLoaderRunner : MonoBehaviour
    {
        private static AddressableLabelLoaderRunner _instance;

        internal static Coroutine Run(IEnumerator routine)
        {
            if (_instance == null)
            {
                GameObject host = new GameObject("[AddressableLabelLoader]");
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<AddressableLabelLoaderRunner>();
            }

            return _instance.StartCoroutine(routine);
        }
    }
}
