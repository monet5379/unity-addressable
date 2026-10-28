using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AddressableLayout.Resource
{
    internal static class AddressableLabelLoadByType
    {
        private static readonly MethodInfo CountLabelLocationsMethod = typeof(AddressableAssetManager).GetMethod(
            nameof(AddressableAssetManager.CountLabelLocations),
            BindingFlags.Instance | BindingFlags.Public);

        private static readonly MethodInfo LoadLabelThunkMethod = typeof(AddressableLabelLoadByType).GetMethod(
            nameof(LoadLabelThunk),
            BindingFlags.Static | BindingFlags.NonPublic);

        internal static Type Normalize(Type assetType)
        {
            if (assetType == null || !typeof(UnityEngine.Object).IsAssignableFrom(assetType))
            {
                return typeof(UnityEngine.Object);
            }

            return assetType;
        }

        internal static IEnumerator CountLabelLocations(
            AddressableAssetManager manager,
            Type assetType,
            IReadOnlyList<string> labels,
            IDictionary<string, int> results,
            LabelLoadMode mode)
        {
            Type type = Normalize(assetType);
            MethodInfo generic = CountLabelLocationsMethod.MakeGenericMethod(type);
            return (IEnumerator)generic.Invoke(manager, new object[] { labels, results, mode });
        }

        internal static IEnumerator LoadLabel(
            AddressableAssetManager manager,
            Type assetType,
            string label,
            LabelLoadMode mode,
            Action<float> onProgress,
            bool failOnEmptyInDev,
            Action<bool> onAcquired)
        {
            Type type = Normalize(assetType);
            MethodInfo generic = LoadLabelThunkMethod.MakeGenericMethod(type);
            return (IEnumerator)generic.Invoke(
                null,
                new object[] { manager, label, mode, onProgress, failOnEmptyInDev, onAcquired });
        }

        private static IEnumerator LoadLabelThunk<T>(
            AddressableAssetManager manager,
            string label,
            LabelLoadMode mode,
            Action<float> onProgress,
            bool failOnEmptyInDev,
            Action<bool> onAcquired) where T : UnityEngine.Object
        {
            bool acquired = false;
            IEnumerator routine = manager.LoadLabel<T>(
                label,
                mode,
                onProgress,
                failOnEmptyInDev,
                (success, _) => acquired = success);
            yield return routine;
            onAcquired?.Invoke(acquired);
        }
    }
}
