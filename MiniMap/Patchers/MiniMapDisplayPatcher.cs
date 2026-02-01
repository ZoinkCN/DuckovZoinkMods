using Duckov.MiniMaps;
using Duckov.MiniMaps.UI;
using Duckov.Utilities;
using MiniMap.Managers;
using MiniMap.Poi;
using MiniMap.Utils;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using ZoinkModdingLibrary.Attributes;
using ZoinkModdingLibrary.Logging;
using ZoinkModdingLibrary.ModSettings;
using ZoinkModdingLibrary.Patcher;
using ZoinkModdingLibrary.Utils;

namespace MiniMap.Patchers
{
    [TypePatcher(typeof(MiniMapDisplay))]
    public class MiniMapDisplayPatcher : PatcherBase
    {
        public static new PatcherBase Instance { get; } = new MiniMapDisplayPatcher();

        private MiniMapDisplayPatcher() { }

        private static bool IsOriginalDisplay(MiniMapDisplay display)
        {
            return display == MinimapManager.OriginalDisplay;
        }

        [MethodPatcher("HandlePointOfInterest", PatchType.Prefix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool HandlePointOfInterestPrefix(MiniMapDisplay __instance, MonoBehaviour poi)
        {
            if (poi == null) return false;
            if (poi is CharacterPoi characterPoi)
            {
                CharacterPoiManager.HandlePointOfInterest(characterPoi, IsOriginalDisplay(__instance));
                return false;
            }
            return true;
        }

        [MethodPatcher("ReleasePointOfInterest", PatchType.Prefix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool ReleasePointOfInterestPrefix(MiniMapDisplay __instance, MonoBehaviour poi)
        {
            if (poi == null) return false;
            if (poi is CharacterPoi characterPoi)
            {
                CharacterPoiManager.ReleasePointOfInterest(characterPoi, IsOriginalDisplay(__instance));
                return false;
            }
            return true;
        }

        [MethodPatcher("HandlePointsOfInterests", PatchType.Postfix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static void HandlePointsOfInterestsPrefix(MiniMapDisplay __instance)
        {
            CharacterPoiManager.HandlePointsOfInterests(IsOriginalDisplay(__instance));
        }

        [MethodPatcher("SetupRotation", PatchType.Prefix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool SetupRotationPrefix(MiniMapDisplay __instance)
        {
            try
            {
                float rotationAngle = ModSettingManager.GetValue<bool>(ModBehaviour.ModInfo, "mapRotation") ? MiniMapCommon.GetMinimapRotation() : MiniMapCommon.originMapZRotation;
                __instance.transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);
                return false;
            }
            catch (Exception e)
            {
                Log.Error($"设置小地图旋转时出错：" + e.ToString());
                return true;
            }
        }

        [MethodPatcher("RegisterEvents", PatchType.Postfix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static void RegisterEventsPostfix(MiniMapDisplay __instance)
        {
            typeof(CharacterPoiManager).BindEvent(nameof(CharacterPoiManager.PoiRegistered), __instance, "HandlePointOfInterest");
            typeof(CharacterPoiManager).BindEvent(nameof(CharacterPoiManager.PoiUnregistered), __instance, "ReleasePointOfInterest");
        }

        [MethodPatcher("UnregisterEvents", PatchType.Postfix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static void UnregisterEventsPostfix(MiniMapDisplay __instance)
        {
            typeof(CharacterPoiManager).UnbindEvent(nameof(CharacterPoiManager.PoiRegistered), __instance, "HandlePointOfInterest");
            typeof(CharacterPoiManager).UnbindEvent(nameof(CharacterPoiManager.PoiUnregistered), __instance, "ReleasePointOfInterest");
        }
    }
}