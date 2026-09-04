using UnityEngine;

namespace AddressableLayout.Demo
{
    /// <summary>
    /// Milestone A–F 샘플. boot / DemoAreaA / DemoAreaB 라벨 또는 Resources 이중 경로용.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DemoBootSample",
        menuName = "Addressable Layout/Demo/Boot Sample")]
    public sealed class DemoBootSample : ScriptableObject
    {
        [SerializeField] private string markerId = "boot-sample";

        public string MarkerId => markerId;
    }
}
