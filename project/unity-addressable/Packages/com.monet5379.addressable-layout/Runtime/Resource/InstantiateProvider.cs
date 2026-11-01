using UnityEngine;

namespace AddressableLayout.Resource
{
    internal sealed class InstantiateProvider : IPrefabInstanceProvider
    {
        internal static readonly InstantiateProvider Instance = new();

        public GameObject Spawn(GameObject prefab, Transform parent) =>
            parent != null
                ? Object.Instantiate(prefab, parent)
                : Object.Instantiate(prefab);

        public void Despawn(GameObject instance) => Object.Destroy(instance);
    }
}
