using Duckov.MiniMaps;
using Duckov.Scenes;
using MiniMap.Poi;
using MiniMap.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace MiniMap.Managers
{
    public static class PoiManager
    {
        private static CharacterSpawnerRoot[]? _cachedSpawnerRoots;
        private static int searchInterval = 1;
        private static int stableCount = 5;
        private static int currentStableCount = 0;

        [Header("探测配置")]
        [SerializeField] private static float probeInterval = 0.2f;      // 探测间隔(秒)
        [SerializeField] private static int maxStableCount = 5;          // 连续稳定次数
        [SerializeField] private static int minExpectedItems = 1;        // 期望最小物品数

        // 状态变量
        private enum FinderState { Probing, Stable, Processing }
        private static FinderState currentState = FinderState.Probing;

        // 探测相关
        private static List<CharacterMainControl>? cachedItems;
        private static int lastItemCount = 0;
        private static int stableProbeCount = 0;
        private static float nextProbeTime = 0f;

        //// 回调
        //public static event Action<Transform[]> OnItemsStabilized;
        //public static event Action<Transform> OnNewItemFound;

        public static void Start()
        {
            StartProbing();
        }

        public static void Update()
        {
            switch (currentState)
            {
                case FinderState.Probing:
                    UpdateProbing();
                    break;

                case FinderState.Stable:
                    ProcessItems();
                    break;

                case FinderState.Processing:
                    // 可以在这里添加其他逻辑
                    break;
            }
        }

        #region 探测阶段
        private static void StartProbing()
        {
            currentState = FinderState.Probing;
            lastItemCount = 0;
            stableProbeCount = 0;
            nextProbeTime = Time.time;
        }

        private static void UpdateProbing()
        {
            if (Time.time < nextProbeTime) return;

            ProbeItems();
            nextProbeTime = Time.time + probeInterval;
        }

        private static void ProbeItems()
        {
            if (MultiSceneCore.Instance == null)
            {
                return;
            }

            var newItems = MultiSceneCore.Instance.GetComponentsInChildren<CharacterMainControl>(true);
            int currentCount = newItems.Length;

            // 检查物品数量变化
            if (currentCount > lastItemCount)
            {
                // 发现新物品
                stableProbeCount = 0;

                // 更新缓存
                lastItemCount = currentCount;
            }
            else
            {
                // 数量稳定
                stableProbeCount++;
                // 检查是否达到稳定条件
                if (stableProbeCount >= maxStableCount || ShouldAcceptCurrentCount())
                {
                    cachedItems = newItems.ToList();
                    currentState = FinderState.Stable;
                }
            }
        }

        private static bool ShouldAcceptCurrentCount()
        {
            return Time.timeSinceLevelLoad > 10f; // 场景加载10秒后接受任何数量
        }
        #endregion

        #region 处理阶段
        private static void ProcessItems()
        {
            if (cachedItems == null || cachedItems.Count == 0) return;
            cachedItems.RemoveAll(s => s == null || s.IsDestroyed());
            bool showOnlyActivated = ModSettingManager.GetValue("showOnlyActivated", false);
            bool showPetPoi = ModSettingManager.GetValue("showPetPoi", true);
            bool showInMap = ModSettingManager.GetValue("showPoiInMap", true);
            bool showInMiniMap = ModSettingManager.GetValue("showPoiInMiniMap", true);
            foreach (var character in cachedItems)
            {
                if (character != null && character.gameObject.activeInHierarchy)
                {
                    PoiCommon.CreatePoiIfNeeded(character, out IPointOfInterest? cPoi, out IPointOfInterest? dPoi);
                    if (cPoi is CharacterPointOfInterestBase characterPoi)
                    {
                        characterPoi.SetShows(showOnlyActivated, showInMap, showInMiniMap, showPetPoi);
                    }
                    if (dPoi is CharacterPointOfInterestBase directionPoi)
                    {
                        directionPoi.SetShows(showOnlyActivated, showInMap, showInMiniMap, showPetPoi);
                    }
                }
            }
        }
        #endregion

        private static IEnumerable<CharacterMainControl> EnumerateSpawnedCharacters()
        {
            var roots = GetSpawnerRoots();
            if (roots == null || roots.Length == 0)
                yield break;

            FieldInfo? fieldInfo = typeof(CharacterSpawnerRoot).GetField("createdCharacters", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (CharacterSpawnerRoot root in roots)
            {
                if (fieldInfo?.GetValue(root) is not List<CharacterMainControl> list)
                    continue;

                foreach (var character in list)
                {
                    if (IsCharacterValid(character))
                    {
                        yield return character;
                    }
                }
            }
        }

        private static CharacterSpawnerRoot[] GetSpawnerRoots()
        {
            //if (_cachedSpawnerRoots == null || _cachedSpawnerRoots.Length == 0 || Array.Exists(_cachedSpawnerRoots, r => r == null))
            //{
            _cachedSpawnerRoots = Resources.FindObjectsOfTypeAll<CharacterSpawnerRoot>() ?? Array.Empty<CharacterSpawnerRoot>();
            //}

            return _cachedSpawnerRoots;
        }

        private static bool IsCharacterValid(CharacterMainControl character)
        {
            if (character == null)
                return false;

            var go = character.gameObject;
            if (!go.scene.IsValid() || !go.scene.isLoaded)
                return false;

            if (character.Health == null || character.Health.IsDead)
                return false;

            return true;
        }
    }
}
