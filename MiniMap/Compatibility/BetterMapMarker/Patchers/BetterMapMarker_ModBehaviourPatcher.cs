using Duckov.MiniMaps;
using System.Reflection;
using ZoinkModdingLibrary.Attributes;
using ZoinkModdingLibrary.Patcher;

namespace MiniMap.Compatibility.BetterMapMarker.Patchers
{
    [TypePatcher("BetterMapMarker", "BetterMapMarker.ModBehaviour")]
    public class BetterMapMarker_ModBehaviourPatcher : CompatibilityPatcherBase
    {
        public static new BetterMapMarker_ModBehaviourPatcher Instance { get; } = new BetterMapMarker_ModBehaviourPatcher();
        protected override List<PatcherBase>? SubPatchers { get; } = new List<PatcherBase>()
        {
            BetterMapMarker_MiniMapDisplayPatcher.Instance
        };
        private BetterMapMarker_ModBehaviourPatcher() { }

        [MethodPatcher("UpdateMarker", PatchType.Prefix, BindingFlags.Instance | BindingFlags.NonPublic)]
        public static void UpdateMarkerPrefix(object marker)
        {
            SimplePointOfInterest? poi = AssemblyOption.GetField<SimplePointOfInterest>(marker, "Poi");
            if (poi != null)
            {
                poi.name = poi.name.Replace("CharacterMarker", "LootboxMarker");
            }
        }
    }
}
