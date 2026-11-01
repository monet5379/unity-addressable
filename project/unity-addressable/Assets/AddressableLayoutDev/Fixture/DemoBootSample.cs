using UnityEngine;

namespace AddressableLayout.Demo
{
    [CreateAssetMenu(
        fileName = "DemoBootSample",
        menuName = "Addressable Layout/Demo/Boot Sample")]
    public sealed class DemoBootSample : ScriptableObject
    {
        [SerializeField] private string markerId = "boot-sample";

        public string MarkerId => markerId;
    }
}
