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

// This hooks into the CCK4+ build pipeline (CVR.CCKEditor.ContentBuilder.CCKBuildProcessor).
// For the pre-CCK4 build pipeline, see ChillaxinsPreBuildAvatar.cs.
#if CVR_CCK_EXISTS && CVR_CCK_4_OR_NEWER && CHILLAXINS_NDMF_IS_INSTALLED
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;
using nadena.dev.ndmf;

namespace Hai.Chillaxins
{
    [InitializeOnLoad]
    public class ChillaxinsPreBuildAvatarNew
    {
        // The CCK4+ build processor assembly isn't referenceable from this package's asmdef
        // (it has no asmdef of its own, so it compiles into the default editor assembly),
        // so we locate it via reflection and synthesize a subclass at runtime instead. CCK
        // auto-discovers CCKBuildProcessor subclasses by scanning all loaded types.
        private const string BuildProcessorBase_ClassFullName = "CVR.CCKEditor.ContentBuilder.CCKBuildProcessor";
        private const string BuildProcessorSettings_ClassFullName = "CVR.CCKEditor.ContentBuilder.CCKBuildProcessorSettings";
        private const string OnPreProcessAvatar_MethodName = "OnPreProcessAvatar";
        private const string DynamicAssemblyName = "Hai.Chillaxins.Dynamic";
        private const string DynamicTypeName = "Hai.Chillaxins.Dynamic.ChillaxinsNdmfBuildProcessor";

        // Same preference key as the legacy Pre-Avatar Bundle Event, so the existing menu toggle governs both.
        private const string DisablePreAvatarBundleEventKey = "Chillaxins_DisablePreAvatarBundleEvent";

        static ChillaxinsPreBuildAvatarNew()
        {
            if (Application.isPlaying) return;

            // Check if the feature is disabled
            if (EditorPrefs.GetBool(DisablePreAvatarBundleEventKey, true)) // Default is disabled
            {
                Debug.Log("(Chillaxins) Pre-Avatar Bundle Event is disabled.");
                return;
            }

            var t_CCKBuildProcessor = FindTypeByFullName(BuildProcessorBase_ClassFullName);
            if (t_CCKBuildProcessor == null)
            {
                Debug.LogWarning("(Chillaxins) Failed to find CVR.CCKEditor.ContentBuilder.CCKBuildProcessor");
                return;
            }

            try
            {
                EmitAndRegisterBuildProcessor(t_CCKBuildProcessor);
                Debug.Log($"(Chillaxins) Found {BuildProcessorBase_ClassFullName}, registered a dynamic build processor to run NDMF...");
            }
            catch (Exception e)
            {
                Debug.LogError($"(Chillaxins) Failed to register a CCKBuildProcessor for NDMF: {e}");
            }
        }

        private static Type FindTypeByFullName(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.FullName == fullName);
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
        }

        private static void EmitAndRegisterBuildProcessor(Type baseType)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(DynamicAssemblyName), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(DynamicAssemblyName);

            var typeBuilder = moduleBuilder.DefineType(DynamicTypeName,
                TypeAttributes.Public | TypeAttributes.Class, baseType);

            var baseMethod = baseType.GetMethod(OnPreProcessAvatar_MethodName, new[] { typeof(GameObject) });
            // Must be public: the emitted type lives in a separate dynamic assembly and can't call a private/internal method.
            var callback = typeof(ChillaxinsPreBuildAvatarNew).GetMethod(nameof(OnPreProcessAvatarCallback),
                BindingFlags.Static | BindingFlags.Public);

            var methodBuilder = typeBuilder.DefineMethod(OnPreProcessAvatar_MethodName,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(void), new[] { typeof(GameObject) });

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, callback);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, baseMethod);
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            var createdType = typeBuilder.CreateType();

            // CCK only picks up newly-discovered processors as enabled once its own delayed
            // scan runs; force it on now instead of relying on that scan winning the race.
            ForceEnableProcessor(createdType.FullName);
        }

        private static void ForceEnableProcessor(string processorTypeName)
        {
            var t_Settings = FindTypeByFullName(BuildProcessorSettings_ClassFullName);
            if (t_Settings == null) return;

            var p_Instance = t_Settings.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var instance = p_Instance?.GetValue(null);
            if (instance == null) return;

            var m_SetProcessorEnabled = t_Settings.GetMethod("SetProcessorEnabled", BindingFlags.Public | BindingFlags.Instance);
            if (m_SetProcessorEnabled == null) return;

            m_SetProcessorEnabled.Invoke(instance, new object[] { processorTypeName, true });
        }

        // Public because the emitted dynamic type is in a separate assembly and must be able to call this.
        public static void OnPreProcessAvatarCallback(GameObject avatar)
        {
            try
            {
                Debug.Log("(Chillaxins) Running NDMF...");
                AvatarProcessor.ProcessAvatar(avatar);
                RemoveMissingScriptsRecursively(avatar);
            }
            catch (Exception e)
            {
                Debug.LogError($"(Chillaxins) NDMF processing failed: {e}");
            }
        }

        // NDMF plugins can leave behind components with missing scripts (e.g. destroyed build-time-only
        // components); CCK refuses to save a prefab containing any, so strip them before it tries to.
        private static void RemoveMissingScriptsRecursively(GameObject avatar)
        {
            foreach (var transform in avatar.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject) > 0)
                {
                    Debug.LogWarning($"(Chillaxins) Removed missing script(s) from '{transform.gameObject.name}' after NDMF processing.");
                }
            }
        }
    }
}
#endif
