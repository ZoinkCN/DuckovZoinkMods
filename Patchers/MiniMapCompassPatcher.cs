using Duckov.MiniMaps;
using MiniMap.Managers;
using MiniMap.Utils;
using System.Reflection;
using UnityEngine;

namespace MiniMap.Patchers
{
    public class MiniMapCompassPatcher : PatcherBase
    {
        public static new PatcherBase Instance { get; } = new MiniMapCompassPatcher();
        public override Type? TargetType => typeof(MiniMapCompass);

        private MiniMapCompassPatcher() { }
        static FieldInfo? arrowField;

        [BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)]
        public static bool Patcher_SetupRotation_Prefix(MiniMapCompass __instance)
        {
            try
            {
                if (arrowField == null)
                {
                    arrowField = typeof(MiniMapCompass).GetField("arrow", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (arrowField == null)
                    {
                        Debug.Log($"[{ModBehaviour.MOD_NAME}] 无法获取指南针对象");
                    }
                }

                Transform? trans = arrowField?.GetValue(__instance) as Transform;
                if (trans == null)
                {
                    return false;
                }
                trans.localRotation = ModSettingManager.GetValue<bool>("mapRotation")
                    ? MiniMapCommon.GetChracterRotation()
                    : Quaternion.Euler(0f, 0f, MiniMapCommon.originMapZRotation);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ModBehaviour.MOD_NAME}] 设置指南针旋转时出错：" + e.ToString());
                return true;
            }
        }

    }
}

