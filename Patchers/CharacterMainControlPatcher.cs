using MiniMap.Utils;
using System.Reflection;
using UnityEngine;

namespace MiniMap.Patchers
{
    public class CharacterMainControlPatcher : PatcherBase
    {
        public static new PatcherBase Instance { get; } = new CharacterMainControlPatcher();
        public override Type? TargetType => typeof(CharacterMainControl);
        private CharacterMainControlPatcher() { }

        [BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)]
        public static void Patch_Update_Postfix(CharacterMainControl __instance)
        {
            try
            {
                PoiCommon.CreatePoiIfNeeded(__instance, out _, out _);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ModBehaviour.MOD_NAME}] characterPoi update failed: {e.Message}");
            }
        }
    }
}
