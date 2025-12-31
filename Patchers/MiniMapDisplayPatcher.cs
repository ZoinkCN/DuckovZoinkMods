using Duckov.MiniMaps.UI;
using MiniMap.Managers;
using MiniMap.Poi;
using MiniMap.Utils;
using System.Reflection;
using UnityEngine;

namespace MiniMap.Patchers
{
    public class MiniMapDisplayPatcher : PatcherBase
    {
        public static new PatcherBase Instance { get; } = new MiniMapDisplayPatcher();
        public override Type? TargetType => typeof(MiniMapDisplay);

        private MiniMapDisplayPatcher() { }
        [BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool Patcher_HandlePointOfInterest_Prefix(MiniMapDisplay __instance, MonoBehaviour poi)
        {
            if (poi is CharacterPointOfInterestBase characterPoi)
            {
                return (__instance == CustomMinimapManager.OriginalMinimapDisplay && characterPoi.ShowInMap)
                    || (__instance == CustomMinimapManager.DuplicatedMinimapDisplay && characterPoi.ShowInMiniMap);
            }
            return true;
        }

        [BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool Patcher_SetupRotation_Prefix(MiniMapDisplay __instance)
        {
            try
            {
                __instance.transform.rotation = ModSettingManager.GetValue<bool>("mapRotation")
                    ? MiniMapCommon.GetPlayerMinimapRotationInverse()
                    : Quaternion.Euler(0f, 0f, MiniMapCommon.originMapZRotation);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ModBehaviour.MOD_NAME}] 设置小地图旋转时出错：" + e.ToString());
                return true;
            }
        }
    }
}

