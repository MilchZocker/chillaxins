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
using Hai.Chillaxins;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

[assembly: ExportsPlugin(typeof(ChillaxinsSkipIfPlayModeBuildPlugin))]
namespace Hai.Chillaxins
{
    public class ChillaxinsSkipIfPlayModeBuildPlugin : Plugin<ChillaxinsSkipIfPlayModeBuildPlugin>
    {
        public override string QualifiedName => "Hai.Chillaxins.ChillaxinsSkipIfPlayModeBuildPlugin";

        // New preference key to disable Play Mode Upload Trigger
        private const string DisablePlayModeUploadTriggerKey = "Chillaxins_DisablePlayModeUploadTrigger";

        protected override void Configure()
        {
            // Check if the feature is disabled
            if (EditorPrefs.GetBool(DisablePlayModeUploadTriggerKey, true)) // Default is disabled
            {
                Debug.Log("(Chillaxins) Play Mode Upload Trigger is disabled.");
                return;
            }

            // This is a hack, as NDMF processes the avatar when entering Play Mode as the result of an upload.
            InPhase(BuildPhase.Resolving)
                .Run("Chillaxins: If this is a Play Mode build, skip NDMF", EvaluateSkip);
        }

        private void EvaluateSkip(BuildContext context)
        {
            if (!Application.isPlaying) return;
            if (!EditorPrefs.GetBool("m_ABI_isBuilding")) return;

            Debug.Log("(Chillaxins) Detected a Play Mode build, skipping avatar...");

            context.AvatarRootObject.SetActive(false);
            var avatarForScreenshots = Object.Instantiate(context.AvatarRootObject);
            foreach (var comp in avatarForScreenshots.GetComponents<Component>())
            {
                if (comp.GetType().FullName == "ABI.CCK.Components.CVRAvatar") Object.DestroyImmediate(comp);
            }
            foreach (var animator in avatarForScreenshots.GetComponents<Animator>())
            {
                Object.DestroyImmediate(animator);
            }
            avatarForScreenshots.SetActive(true);

            var allTransforms = context.AvatarRootTransform.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t && t != context.AvatarRootTransform) Object.DestroyImmediate(t.gameObject);
            }
        }
    }
}
#endif
