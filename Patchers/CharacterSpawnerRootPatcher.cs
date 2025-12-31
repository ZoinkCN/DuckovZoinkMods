using MiniMap.Utils;
using System.Reflection;
using UnityEngine;

namespace MiniMap.Patchers
{
    public class CharacterSpawnerRootPatcher : PatcherBase
    {
        public static new PatcherBase Instance { get; } = new CharacterSpawnerRootPatcher();
        public override Type? TargetType => typeof(CharacterSpawnerRoot);

        private CharacterSpawnerRootPatcher() { }

        [BindingFlags(BindingFlags.Instance | BindingFlags.Public)]
        public static bool Patch_AddCreatedCharacter_Prefix(CharacterMainControl c)
        {
            try
            {
                PoiCommon.CreatePoiIfNeeded(c, out _, out _);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ModBehaviour.MOD_NAME}] characterPoi add failed: {e.Message}");
            }
            return true;
        }
    }
}
