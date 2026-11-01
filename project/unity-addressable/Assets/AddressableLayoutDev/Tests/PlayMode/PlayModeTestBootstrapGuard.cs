using AddressableLayout.Demo;
using UnityEngine;

namespace AddressableLayout.Tests.PlayMode
{
    internal static class PlayModeTestBootstrapGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SuppressDemoAutoHost()
        {
            DemoPlayModeVerification.DisableAutoBootstrap = true;
        }
    }
}
