using UnityEngine;

namespace AddressableLayout.Demo
{
    /// <summary>
    /// Milestone A–C 샘플. boot / DemoAreaA / DemoAreaB 라벨로 등록한다.
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
