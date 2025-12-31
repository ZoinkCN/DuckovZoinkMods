using Duckov.MiniMaps.UI;
using MiniMap.Poi;
using MiniMap.Utils;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMap.Patchers
{
    public class PointOfInterestEntryPatcher : PatcherBase
    {
        public static new PatcherBase Instance { get; } = new PointOfInterestEntryPatcher();
        public override Type? TargetType => typeof(PointOfInterestEntry);
        private PointOfInterestEntryPatcher() { }

        [BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool Patch_UpdateRotation_Prefix(PointOfInterestEntry __instance, MiniMapDisplayEntry ___minimapEntry)
        {
            try
            {
                if (__instance.Target is DirectionPointOfInterest poi)
                {
                    MiniMapDisplay? display = ___minimapEntry.GetComponentInParent<MiniMapDisplay>();
                    if (display == null)
                    {
                        return true;
                    }
                    __instance.transform.rotation = Quaternion.Euler(0f, 0f, poi.RealEulerAngle + display.transform.rotation.eulerAngles.z);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ModBehaviour.MOD_NAME}] PointOfInterestEntry UpdateRotation failed: {e.Message}");
                return true;
            }
        }

        [BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool Patch_Update_Prefix(PointOfInterestEntry __instance, Image ___icon, MiniMapDisplay ___master)
        {
            CharacterMainControl? character = __instance.Target?.GetComponent<CharacterMainControl>();
            if (character != null && !character.IsMainCharacter && PoiCommon.IsDead(character))
            {
                GameObject.Destroy(__instance.gameObject);
                return false;
            }

            RectTransform icon = ___icon.rectTransform;
            RectTransform? layout = icon.parent as RectTransform;
            if (layout == null) { return true; }
            if (layout.localPosition + icon.localPosition != Vector3.zero)
            {
                layout.localPosition = Vector3.zero - icon.localPosition;
            }

            return true;
        }
    }
}
