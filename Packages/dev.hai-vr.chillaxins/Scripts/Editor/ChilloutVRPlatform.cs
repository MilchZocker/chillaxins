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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using nadena.dev.ndmf.runtime;
using UnityEditor;
using UnityEngine;

namespace Hai.Chillaxins
{
    [NDMFPlatformProvider]
    public sealed class ChilloutVRPlatform : INDMFPlatformProvider
    {
        public static ChilloutVRPlatform Instance { get; } = new ChilloutVRPlatform();

        private static Type _cvrAvatarType;
        private static bool _cvrAvatarTypeSearched;

        public static Type CVRAvatarType
        {
            get
            {
                if (!_cvrAvatarTypeSearched)
                {
                    _cvrAvatarTypeSearched = true;
                    _cvrAvatarType = Type.GetType("ABI.CCK.Components.CVRAvatar, Assembly-CSharp")
                        ?? AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(SafeGetTypes)
                            .FirstOrDefault(t => t.FullName == "ABI.CCK.Components.CVRAvatar");
                }
                return _cvrAvatarType;
            }
        }

        public static bool IsManualBake
        {
            get
            {
                var frames = new System.Diagnostics.StackTrace().GetFrames();
                if (frames == null) return false;
                foreach (var frame in frames)
                {
                    var method = frame.GetMethod();
                    if (method != null && method.DeclaringType?.FullName == "nadena.dev.ndmf.AvatarProcessor")
                    {
                        if (method.Name == "ManualProcessAvatar" || method.Name == "ProcessAvatarUI")
                            return true;
                    }
                }
                return false;
            }
        }

        private static bool HasVRChatPlatform =>
            Type.GetType("nadena.dev.ndmf.vrchat.VRChatPlatform, nadena.dev.ndmf.vrchat") != null;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            var avatarType = CVRAvatarType;
            if (avatarType != null)
            {
                // Ensure CVRAvatar is registered in RuntimeUtil.AllRootTypes regardless of platform registration order
                try
                {
                    var field = typeof(RuntimeUtil).GetField("AllRootTypes", BindingFlags.NonPublic | BindingFlags.Static);
                    if (field?.GetValue(null) is HashSet<Type> allRootTypes)
                    {
                        allRootTypes.Add(avatarType);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"(Chillaxins) Failed to register CVRAvatar in RuntimeUtil.AllRootTypes: {e.Message}");
                }
            }

            // Set default platform if VRChat SDK is not active
            if (!HasVRChatPlatform)
            {
                try
                {
                    typeof(AmbientPlatform).GetProperty("DefaultPlatform", BindingFlags.Public | BindingFlags.Static)?
                        .GetSetMethod(true)?
                        .Invoke(null, new object[] { Instance });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"(Chillaxins) Failed to set AmbientPlatform.DefaultPlatform: {e.Message}");
                }
            }
        }

        // When VRChat SDK is absent (normal ChilloutVR project), we identify as WellKnownPlatforms.VRChatAvatar30
        // so that passes from NDMF plugins (Modular Avatar, AAO, etc.) targeting VRChat Avatar 3.0 run seamlessly.
        public string QualifiedName => HasVRChatPlatform ? "com.alphablend.chilloutvr" : WellKnownPlatforms.VRChatAvatar30;
        public string DisplayName => "ChilloutVR";
        public Texture2D Icon => null;
        public Type AvatarRootComponentType => CVRAvatarType;
        public bool HasNativeConfigData => true;

        public bool HasNativeUI => FindControlPanelType() != null;

        public void OpenNativeUI()
        {
            var panelType = FindControlPanelType();
            if (panelType != null)
            {
                var showWindow = panelType.GetMethod("ShowWindow", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (showWindow != null)
                {
                    showWindow.Invoke(null, null);
                }
                else
                {
                    EditorWindow.GetWindow(panelType);
                }
            }
        }

        public CommonAvatarInfo ExtractCommonAvatarInfo(GameObject avatarRoot)
        {
            var cai = new CommonAvatarInfo();
            var cvrAvatarType = CVRAvatarType;
            if (cvrAvatarType == null) return cai;

            var cvrAvatar = avatarRoot.GetComponent(cvrAvatarType);
            if (cvrAvatar == null) return cai;

            try
            {
                var fViewPos = cvrAvatarType.GetField("viewPosition", BindingFlags.Public | BindingFlags.Instance);
                if (fViewPos != null && fViewPos.GetValue(cvrAvatar) is Vector3 viewPos)
                {
                    cai.EyePosition = viewPos;
                }

                var fBodyMesh = cvrAvatarType.GetField("bodyMesh", BindingFlags.Public | BindingFlags.Instance);
                var fVisemeBlendshapes = cvrAvatarType.GetField("visemeBlendshapes", BindingFlags.Public | BindingFlags.Instance);
                var fUseVisemeLipsync = cvrAvatarType.GetField("useVisemeLipsync", BindingFlags.Public | BindingFlags.Instance);

                var bodyMesh = fBodyMesh?.GetValue(cvrAvatar) as SkinnedMeshRenderer;
                var useVisemes = fUseVisemeLipsync != null && (bool)fUseVisemeLipsync.GetValue(cvrAvatar);
                var visemes = fVisemeBlendshapes?.GetValue(cvrAvatar) as string[];

                if (bodyMesh != null)
                {
                    cai.VisemeRenderer = bodyMesh;

                    if (useVisemes && visemes != null)
                    {
                        string[] oculusVisemeNames = {
                            CommonAvatarInfo.Viseme_Silence,
                            CommonAvatarInfo.Viseme_PP,
                            CommonAvatarInfo.Viseme_FF,
                            CommonAvatarInfo.Viseme_TH,
                            CommonAvatarInfo.Viseme_DD,
                            CommonAvatarInfo.Viseme_kk,
                            CommonAvatarInfo.Viseme_CH,
                            CommonAvatarInfo.Viseme_SS,
                            CommonAvatarInfo.Viseme_nn,
                            CommonAvatarInfo.Viseme_RR,
                            CommonAvatarInfo.Viseme_aa,
                            CommonAvatarInfo.Viseme_E,
                            CommonAvatarInfo.Viseme_ih,
                            CommonAvatarInfo.Viseme_oh,
                            CommonAvatarInfo.Viseme_ou
                        };

                        for (int i = 0; i < Math.Min(visemes.Length, oculusVisemeNames.Length); i++)
                        {
                            if (!string.IsNullOrEmpty(visemes[i]))
                            {
                                cai.VisemeBlendshapes[oculusVisemeNames[i]] = visemes[i];
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"(Chillaxins) Failed to extract CommonAvatarInfo: {e.Message}");
            }

            return cai;
        }

        public bool CanInitFromCommonAvatarInfo(GameObject avatarRoot, CommonAvatarInfo info)
        {
            return CVRAvatarType != null;
        }

        public void InitFromCommonAvatarInfo(GameObject avatarRoot, CommonAvatarInfo cai)
        {
            var cvrAvatarType = CVRAvatarType;
            if (cvrAvatarType == null) return;

            var cvrAvatar = avatarRoot.GetComponent(cvrAvatarType);
            if (cvrAvatar == null)
            {
                cvrAvatar = avatarRoot.AddComponent(cvrAvatarType);
            }

            if (cai.EyePosition.HasValue)
            {
                var fViewPos = cvrAvatarType.GetField("viewPosition", BindingFlags.Public | BindingFlags.Instance);
                fViewPos?.SetValue(cvrAvatar, cai.EyePosition.Value);
            }

            if (cai.VisemeRenderer != null)
            {
                var fBodyMesh = cvrAvatarType.GetField("bodyMesh", BindingFlags.Public | BindingFlags.Instance);
                var fUseVisemeLipsync = cvrAvatarType.GetField("useVisemeLipsync", BindingFlags.Public | BindingFlags.Instance);
                fBodyMesh?.SetValue(cvrAvatar, cai.VisemeRenderer);
                fUseVisemeLipsync?.SetValue(cvrAvatar, true);
            }

            if (cai.VisemeBlendshapes.Count > 0)
            {
                var fVisemeBlendshapes = cvrAvatarType.GetField("visemeBlendshapes", BindingFlags.Public | BindingFlags.Instance);
                var visemes = fVisemeBlendshapes?.GetValue(cvrAvatar) as string[];
                if (visemes == null || visemes.Length < 15)
                {
                    visemes = new string[15];
                }

                string[] oculusVisemeNames = {
                    CommonAvatarInfo.Viseme_Silence,
                    CommonAvatarInfo.Viseme_PP,
                    CommonAvatarInfo.Viseme_FF,
                    CommonAvatarInfo.Viseme_TH,
                    CommonAvatarInfo.Viseme_DD,
                    CommonAvatarInfo.Viseme_kk,
                    CommonAvatarInfo.Viseme_CH,
                    CommonAvatarInfo.Viseme_SS,
                    CommonAvatarInfo.Viseme_nn,
                    CommonAvatarInfo.Viseme_RR,
                    CommonAvatarInfo.Viseme_aa,
                    CommonAvatarInfo.Viseme_E,
                    CommonAvatarInfo.Viseme_ih,
                    CommonAvatarInfo.Viseme_oh,
                    CommonAvatarInfo.Viseme_ou
                };

                for (int i = 0; i < oculusVisemeNames.Length; i++)
                {
                    if (cai.VisemeBlendshapes.TryGetValue(oculusVisemeNames[i], out var shapeName))
                    {
                        visemes[i] = shapeName;
                    }
                }

                fVisemeBlendshapes?.SetValue(cvrAvatar, visemes);
            }
        }

        private static Type FindControlPanelType()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.FullName == "CVR.CCKEditor.GUI.CCKControlPanel"
                                  || t.FullName == "ABI.CCK.Scripts.Editor.CCK_BuildManagerWindow");
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
}
#endif

