using UnityEngine;

namespace AddressableLayout.Resource
{
    public interface IPrefabInstanceProvider
    {
        GameObject Spawn(GameObject prefab, Transform parent);

        void Despawn(GameObject instance);
    }
}
