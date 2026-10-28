using System;
using System.Collections;
using UnityEngine;

namespace AddressableLayout.Demo
{
    public sealed class DemoAddressableHost : MonoBehaviour
    {
        private const int BootstrapFrameDelay = 3;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ScheduleBootstrap()
        {
            if (FindFirstObjectByType<DemoAddressableHostBootstrapRunner>() != null)
            {
                return;
            }

            var scheduler = new GameObject("AddressableLayoutDemoBootstrap");
            scheduler.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(scheduler);
            scheduler.AddComponent<DemoAddressableHostBootstrapRunner>();
        }

        private async void Start()
        {
            if (DemoPlayModeVerification.DisableAutoBootstrap)
            {
                return;
            }

            try
            {
                await DemoPlayModeVerification.RunAllAsync();
                Debug.Log("[DemoAddressableHost] All milestones OK.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DemoAddressableHost] {ex.Message}");
            }
        }

        private sealed class DemoAddressableHostBootstrapRunner : MonoBehaviour
        {
            private IEnumerator Start()
            {
                for (int i = 0; i < BootstrapFrameDelay; i++)
                {
                    if (DemoPlayModeVerification.DisableAutoBootstrap)
                    {
                        Destroy(gameObject);
                        yield break;
                    }

                    yield return null;
                }

                if (DemoPlayModeVerification.DisableAutoBootstrap)
                {
                    Destroy(gameObject);
                    yield break;
                }

                if (FindFirstObjectByType<DemoAddressableHost>() != null)
                {
                    Destroy(gameObject);
                    yield break;
                }

                var go = new GameObject("AddressableLayoutDemo");
                DontDestroyOnLoad(go);
                go.AddComponent<DemoAddressableHost>();
                Destroy(gameObject);
            }
        }
    }
}
