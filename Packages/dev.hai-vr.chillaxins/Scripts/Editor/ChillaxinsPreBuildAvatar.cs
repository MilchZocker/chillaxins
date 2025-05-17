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

#if CVR_CCK_EXISTS && CHILLAXINS_NDMF_IS_INSTALLED
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using nadena.dev.ndmf;

namespace Hai.Chillaxins
{
    [InitializeOnLoad]
    public class ChillaxinsPreBuildAvatar
    {
        private const string BuildUtility_ClassFullName = "ABI.CCK.Scripts.Editor.CCK_BuildUtility";
        private const string PreAvatarBundleEvent_FieldName = "PreAvatarBundleEvent";

        // New preference key to disable Pre-Avatar Bundle Event
        private const string DisablePreAvatarBundleEventKey = "Chillaxins_DisablePreAvatarBundleEvent";

        static ChillaxinsPreBuildAvatar()
        {
            if (Application.isPlaying) return;

            // Check if the feature is disabled
            if (EditorPrefs.GetBool(DisablePreAvatarBundleEventKey, true)) // Default is disabled
            {
                Debug.Log("(Chillaxins) Pre-Avatar Bundle Event is disabled.");
                return;
            }

            UnityEvent<GameObject> preAvatarBundleEvent = FindPreAvatarBundleEventOrNull();
            if (preAvatarBundleEvent == null)
            {
                Debug.LogWarning("(Chillaxins) Failed to find CCK_BuildUtility.PreAvatarBundleEvent");
                return;
            }

            Debug.Log($"(Chillaxins) Found CCK_BuildUtility.PreAvatarBundleEvent ({preAvatarBundleEvent.GetType().FullName}), adding NDMF listener...");
            preAvatarBundleEvent.AddListener(OnPreAvatarBundleEvent);
        }

        private static UnityEvent<GameObject> FindPreAvatarBundleEventOrNull()
        {
            var t_CCK_BuildUtility = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(t => t.FullName == BuildUtility_ClassFullName);
            if (t_CCK_BuildUtility == null) return null;

            var f_PreAvatarBundleEvent = t_CCK_BuildUtility.GetField(PreAvatarBundleEvent_FieldName);

            return (UnityEvent<GameObject>)f_PreAvatarBundleEvent.GetValue(null);
        }

        private static void OnPreAvatarBundleEvent(GameObject avatar)
        {
            Debug.Log("(Chillaxins) Running NDMF...");
            AvatarProcessor.ProcessAvatar(avatar);
        }
    }
}
#endif
