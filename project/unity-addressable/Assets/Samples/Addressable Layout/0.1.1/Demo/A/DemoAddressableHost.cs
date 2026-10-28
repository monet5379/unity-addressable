using System;
using System.Collections;
using UnityEngine;

namespace AddressableLayout.Demo
{
    /// <summary>
    /// Play 시 milestones A–F 검증 (수동 Play용). 자동 테스트는 <see cref="DemoPlayModeVerification"/>.
    /// </summary>
    public sealed class DemoAddressableHost : MonoBehaviour
    {
        /// <summary>
        /// Play Mode 테스트 PrebuildSetup이 AfterSceneLoad보다 늦게 실행되므로, 부트스트랩을 지연한다.
        /// </summary>
        private const int BootstrapFrameDelay = 3;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ScheduleBootstrap()
        {
            if (FindFirstObjectByType<DemoAddressableHostBootstrapRunner>() != null)
            {
                return;
            }

            GameObject scheduler = new("AddressableLayoutDemoBootstrap")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            DontDestroyOnLoad(scheduler);
            _ = scheduler.AddComponent<DemoAddressableHostBootstrapRunner>();
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

                GameObject go = new("AddressableLayoutDemo");
                DontDestroyOnLoad(go);
                go.AddComponent<DemoAddressableHost>();
                Destroy(gameObject);
            }
        }
    }
}
