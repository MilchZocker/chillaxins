using UnityEditor;
using UnityEngine;

namespace Hai.Chillaxins
{
    public static class ChillaxinsEditorMenu
    {
        private const string DisablePreAvatarBundleEventKey = "Chillaxins_DisablePreAvatarBundleEvent";
        private const string DisablePlayModeUploadTriggerKey = "Chillaxins_DisablePlayModeUploadTrigger";

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
    }
}
