using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace AddressableLayout.Resource
{
    internal static class AddressableLabelLoadByType
    {
        // Bound at compile time through an expression tree: renaming or changing the signature of
        // AddressableAssetManager.CountLabelLocations breaks the build here instead of failing at runtime.
        private static readonly MethodInfo CountLabelLocationsMethod = ResolveGenericMethodDefinition(
            (AddressableAssetManager manager) =>
                manager.CountLabelLocations<UnityEngine.Object>(null, null, default(LabelLoadMode)));

        private static readonly MethodInfo LoadLabelThunkMethod = typeof(AddressableLabelLoadByType).GetMethod(
            nameof(LoadLabelThunk),
            BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo ResolveGenericMethodDefinition(
            Expression<Func<AddressableAssetManager, IEnumerator>> call)
        {
            return ((MethodCallExpression)call.Body).Method.GetGenericMethodDefinition();
        }

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
