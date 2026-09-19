// MIT License
// 
// Copyright (c) 2024 Haï~ (@vr_hai github.com/hai-vr)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#if CHILLAXINS_NDMF_IS_INSTALLED
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[assembly: ExportsPlugin(typeof(Hai.Chillaxins.ChillaxinsManualBakeSafeguardPlugin))]

namespace Hai.Chillaxins
{
    [InitializeOnLoad]
    public static class ChillaxinsManualBakeSafeguard
    {
        private static Harmony _harmony;
        private static bool _patched;

        public static bool HasMergeArmaturePlugin => FindMergeArmaturePluginType() != null;

        static ChillaxinsManualBakeSafeguard()
        {
            TryPatch();
            EditorApplication.delayCall += TryPatch;
        }

        public static void TryPatch()
        {
            if (_patched) return;

            var targetType = FindMergeArmaturePluginType();
            if (targetType == null) return;

            try
            {
                var targetMethod = targetType.GetMethod("IsInsideNdmfManualBake", BindingFlags.NonPublic | BindingFlags.Static);
                if (targetMethod == null) return;

                _harmony = new Harmony("dev.hai-vr.chillaxins.manualbakesafeguard");
                var prefixMethod = typeof(ChillaxinsManualBakeSafeguard).GetMethod(
                    nameof(IsInsideNdmfManualBakePrefix),
                    BindingFlags.NonPublic | BindingFlags.Static
                );

                _harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod));
                _patched = true;
                Debug.Log("(Chillaxins) Patched CVRMergeArmaturePlugin.IsInsideNdmfManualBake with safeguard support.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"(Chillaxins) Failed to patch CVRMergeArmaturePlugin: {e.Message}");
            }
        }

        private static bool IsInsideNdmfManualBakePrefix(ref bool __result)
        {
            // If the safeguard is disabled, let the original method run
            if (!ChillaxinsEditorMenu.IsSafeguardManualBakeAssetEditingEnabled)
            {
                return true;
            }

            // If inside NDMF manual bake, ensure CVRMergeArmaturePlugin recognizes it
            if (ChilloutVRPlatform.IsManualBake)
            {
                __result = true;
                return false; // Skip original method
            }

            return true;
        }

        private static Type FindMergeArmaturePluginType()
        {
            return Type.GetType("NDMFMerge.Editor.CVRMergeArmaturePlugin, dev.milchzocker.ndmf-merge.Editor")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(SafeGetTypes)
                    .FirstOrDefault(t => t.FullName == "NDMFMerge.Editor.CVRMergeArmaturePlugin");
        }

        private static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }
    }

    public class ChillaxinsManualBakeSafeguardPlugin : Plugin<ChillaxinsManualBakeSafeguardPlugin>
    {
        public override string QualifiedName => "Hai.Chillaxins.ChillaxinsManualBakeSafeguardPlugin";

        private static bool _pausedAssetEditing;

        protected override void Configure()
        {
            // Only active if the optional safeguard is enabled
            if (!ChillaxinsEditorMenu.IsSafeguardManualBakeAssetEditingEnabled)
                return;

            // If CVRMergeArmaturePlugin is present, it handles its own scope via the Harmony patch.
            // Only pause globally when CVRMergeArmaturePlugin is not present.
            if (ChillaxinsManualBakeSafeguard.HasMergeArmaturePlugin)
                return;

            InPhase(BuildPhase.Resolving)
                .Run("Chillaxins: Pause Asset Editing for Manual Bake", ctx =>
                {
                    if (ChilloutVRPlatform.IsManualBake)
                    {
                        AssetDatabase.StopAssetEditing();
                        _pausedAssetEditing = true;
                    }
                });

            InPhase(BuildPhase.PlatformFinish)
                .Run("Chillaxins: Restore Asset Editing for Manual Bake", ctx =>
                {
                    if (_pausedAssetEditing)
                    {
                        _pausedAssetEditing = false;
                        AssetDatabase.StartAssetEditing();
                    }
                });
        }
    }
}
#endif

