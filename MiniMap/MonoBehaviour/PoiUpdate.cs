//using Duckov.Scenes;
//using MiniMap.Poi;
//using UnityEngine;

//namespace Duckov.MiniMaps.UI;

//public class PoiUpdate : MonoBehaviour
//{
//    private MiniMapDisplay? mapDisplay;
//    private bool isMiniMap;

//    private void Awake()
//    {

//    }

//    private void OnEnable()
//    {

//    }

//    private void OnDisable()
//    {

//    }

//    private void Update()
//    {
//        MultiSceneCore multiSceneCore = MultiSceneCore.Instance;
//        IEnumerable<CharacterMainControl> characters = multiSceneCore.GetComponentsInChildren<CharacterMainControl>(true).Where(s => s.transform.parent.gameObject.activeSelf);
//        foreach (CharacterMainControl character in characters)
//        {
//            CharacterPointOfInterestBase[] pois = character.GetComponents<CharacterPointOfInterestBase>();
//            foreach (CharacterPointOfInterestBase poi in pois)
//            {
//                if (isMiniMap && !poi.ShowInMiniMap || !isMiniMap && !poi.ShowInMap)
//                {
//                    poi.ReleasePoi();
//                    continue;
//                }
//                else
//                {
//                    poi.SetupPoi(mapDisplay);
//                }
//            }
//        }
//    }

//    public void Setup(MiniMapDisplay display, bool isMiniMap)
//    {
//        if (mapDisplay == null)
//        {
//            this.mapDisplay = display;
//            this.isMiniMap = isMiniMap;
//        }
//    }

//    private void HandlePointOfInterest(CharacterPointOfInterestBase poi)
//    {

//    }
//}