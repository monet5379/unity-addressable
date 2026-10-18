using UnityEngine;

namespace AddressableLayout.Resource
{
    /// <summary>
    /// <see cref="ResourcesManager.SpawnPrefab"/> / <see cref="ResourcesManager.Despawn"/> 인스턴스 생성·반환 교체점. 풀링은 호스트가 구현.
    /// </summary>
    public interface IPrefabInstanceProvider
    {
        /// <param name="parent">null 가능. 비-null이면 로컬 기준 (instantiateInWorldSpace = false).</param>
        GameObject Spawn(GameObject prefab, Transform parent);

        /// <param name="instance">null 아님 (<see cref="ResourcesManager"/>가 걸러냄).</param>
        void Despawn(GameObject instance);
    }
}
