using UnityEditor;
using UnityEngine;

namespace Hai.Chillaxins
{
    public static class ChillaxinsEditorMenu
    {
        private const string DisablePreAvatarBundleEventKey = "Chillaxins_DisablePreAvatarBundleEvent";
        private const string DisablePlayModeUploadTriggerKey = "Chillaxins_DisablePlayModeUploadTrigger";
        public const string SafeguardManualBakeAssetEditingKey = "Chillaxins_SafeguardManualBakeAssetEditing";

        public static bool IsSafeguardManualBakeAssetEditingEnabled =>
            EditorPrefs.GetBool(SafeguardManualBakeAssetEditingKey, false);

        [MenuItem("Tools/Chillaxins/Disable Pre-Avatar Bundle Event", false, 1)]
        private static void TogglePreAvatarBundleEvent()
        {
            bool currentValue = EditorPrefs.GetBool(DisablePreAvatarBundleEventKey, true);
            EditorPrefs.SetBool(DisablePreAvatarBundleEventKey, !currentValue);
            Debug.Log($"(Chillaxins) Pre-Avatar Bundle Event is now {(currentValue ? "enabled" : "disabled")}.");
        }

        [MenuItem("Tools/Chillaxins/Disable Pre-Avatar Bundle Event", true)]
        private static bool TogglePreAvatarBundleEventValidate()
        {
            Menu.SetChecked("Tools/Chillaxins/Disable Pre-Avatar Bundle Event", EditorPrefs.GetBool(DisablePreAvatarBundleEventKey, true));
            return true;
        }

        [MenuItem("Tools/Chillaxins/Disable Play Mode Upload Trigger", false, 2)]
        private static void TogglePlayModeUploadTrigger()
        {
            bool currentValue = EditorPrefs.GetBool(DisablePlayModeUploadTriggerKey, true);
            EditorPrefs.SetBool(DisablePlayModeUploadTriggerKey, !currentValue);
            Debug.Log($"(Chillaxins) Play Mode Upload Trigger is now {(currentValue ? "enabled" : "disabled")}.");
        }

        [MenuItem("Tools/Chillaxins/Disable Play Mode Upload Trigger", true)]
        private static bool TogglePlayModeUploadTriggerValidate()
        {
            Menu.SetChecked("Tools/Chillaxins/Disable Play Mode Upload Trigger", EditorPrefs.GetBool(DisablePlayModeUploadTriggerKey, true));
            return true;
        }

        [MenuItem("Tools/Chillaxins/Safeguard Manual Bake Asset Editing", false, 3)]
        private static void ToggleSafeguardManualBakeAssetEditing()
        {
            bool currentValue = EditorPrefs.GetBool(SafeguardManualBakeAssetEditingKey, false);
            EditorPrefs.SetBool(SafeguardManualBakeAssetEditingKey, !currentValue);
            Debug.Log($"(Chillaxins) Safeguard Manual Bake Asset Editing is now {(!currentValue ? "enabled" : "disabled")}.");
        }

        [MenuItem("Tools/Chillaxins/Safeguard Manual Bake Asset Editing", true)]
        private static bool ToggleSafeguardManualBakeAssetEditingValidate()
        {
            Menu.SetChecked("Tools/Chillaxins/Safeguard Manual Bake Asset Editing", EditorPrefs.GetBool(SafeguardManualBakeAssetEditingKey, false));
            return true;
        }
    }
}
