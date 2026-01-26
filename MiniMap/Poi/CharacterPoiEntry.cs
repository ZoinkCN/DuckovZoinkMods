using Duckov.Scenes;
using LeTai.TrueShadow;
using MiniMap.Managers;
using MiniMap.Poi;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using Cysharp.Threading.Tasks;

namespace Duckov.MiniMaps.UI
{
    public class CharacterPoiEntry : MonoBehaviour
    {
        // ============ 序列化字段 ============
        private RectTransform? rectTransform;
        private MiniMapDisplay? master;
        private MonoBehaviour? target;
        private CharacterPoiBase? characterPoi;
        private MiniMapDisplayEntry? minimapEntry;
        
        [SerializeField] private Transform? indicatorContainer;
        [SerializeField] private Transform? iconContainer;
        [SerializeField] private Sprite? defaultIcon;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Image? icon;
        [SerializeField] private Transform? direction;
        [SerializeField] private Image? arrow;
        [SerializeField] private TrueShadow? shadow;
        [SerializeField] private TextMeshProUGUI? displayName;
        [SerializeField] private ProceduralImage? areaDisplay;
        [SerializeField] private Image? areaFill;
        [SerializeField] private float areaLineThickness = 1f;
        [SerializeField] private string? caption;

        // ============ 缓存字段 ============
        private Vector3 cachedWorldPosition = Vector3.zero;
        
        /// <summary>
        /// POI缓存数据结构
        /// 存储计算好的位置、旋转、缩放等信息
        /// 完全信任异步处理器，不进行有效性检查
        /// </summary>
        private struct PoiCache
        {
            public Vector2 MapPosition;        // 计算好的地图坐标
            public float RotationAngle;        // 旋转角度（方向图标使用）
            public float ScaleFactor;          // 缩放因子
        }
        
        // 每个POI自己的缓存实例
        private PoiCache _currentCache;
        private long _lastAsyncUpdateTime;
        
        // ============ 公共属性 ============
        public MonoBehaviour? Target => target;
        private float ParentLocalScale => transform.parent.localScale.x;
        
        // ============ 静态字段（场景中心点） ============
        /// <summary>
        /// 当前场景的中心点坐标（用于地图位置计算）
        /// 避免每次计算都使用反射获取
        /// </summary>
        private static Vector3 centerOfObjectScene = Vector3.zero;

        // ============ 公共方法 ============
        
        /// <summary>
        /// 初始化POI条目的UI组件
        /// </summary>
        public void Initialize(CharacterPoiEntryData entryData)
        {
            areaDisplay = entryData.areaDisplay;
            areaFill = entryData.areaFill;
            indicatorContainer = entryData.indicatorContainer;
            iconContainer = entryData.iconContainer;
            icon = entryData.icon;
            shadow = entryData.shadow;
            direction = entryData.direction;
            arrow = entryData.arrow;
            displayName = entryData.displayName;
        }

        /// <summary>
        /// 设置POI条目的基本配置
        /// </summary>
        internal void Setup(MiniMapDisplay master, MonoBehaviour target, MiniMapDisplayEntry minimapEntry)
        {
            rectTransform = transform as RectTransform;
            this.master = master;
            this.target = target;
            this.minimapEntry = minimapEntry;
            this.characterPoi = null;
            
            // 设置默认图标和颜色
            icon.sprite = defaultIcon;
            icon.color = defaultColor;
            areaDisplay.color = defaultColor;
            
            Color color = defaultColor;
            color.a *= 0.1f;
            areaFill.color = color;
            
            caption = target.name;
            icon.gameObject.SetActive(value: true);
            
            // 如果是CharacterPoiBase类型，设置具体属性
            if (target is CharacterPoiBase characterPoi)
            {
                this.characterPoi = characterPoi;
                
                // 设置图标
                icon.gameObject.SetActive(!this.characterPoi.HideIcon);
                icon.sprite = ((characterPoi.Icon != null) ? characterPoi.Icon : defaultIcon);
                icon.color = characterPoi.Color;
                
                // 设置阴影
                if ((bool)shadow)
                {
                    shadow.Color = characterPoi.ShadowColor;
                    shadow.OffsetDistance = characterPoi.ShadowDistance;
                }

                // 设置显示名称
                string value = this.characterPoi.DisplayName;
                caption = characterPoi.DisplayName;
                if (string.IsNullOrEmpty(value))
                {
                    displayName.gameObject.SetActive(value: false);
                }
                else
                {
                    displayName.gameObject.SetActive(value: true);
                    displayName.text = this.characterPoi.DisplayName;
                }

                // 设置区域显示（如果适用）
                if (characterPoi.IsArea)
                {
                    areaDisplay.gameObject.SetActive(value: true);
                    rectTransform.sizeDelta = this.characterPoi.AreaRadius * Vector2.one * 2f;
                    areaDisplay.color = characterPoi.Color;
                    color = characterPoi.Color;
                    color.a *= 0.1f;
                    areaFill.color = color;
                    areaDisplay.BorderWidth = areaLineThickness / ParentLocalScale;
                }
                else
                {
                    icon.enabled = true;
                    areaDisplay.gameObject.SetActive(value: false);
                }

                // 刷新位置并激活对象
                RefreshPosition();
                base.gameObject.SetActive(value: true);
                
                // 初始化缓存
                InitializeCache();
            }
        }

        // ============ 缓存计算方法 ============
        
        /// <summary>
        /// 计算世界坐标对应的地图坐标
        /// </summary>
        private Vector2 CalculateMapPosition(Vector3 worldPosition)
        {
            Vector3 vector = worldPosition - centerOfObjectScene;
            return new Vector2(vector.x, vector.z);
        }
        
        /// <summary>
        /// 计算旋转角度（仅方向图标使用）
        /// </summary>
        private float CalculateRotationAngle()
        {
            if (characterPoi is DirectionPointOfInterest directionPoi)
            {
                return directionPoi.RealEulerAngle;
            }
            return 0f;
        }
        
        // ============ 异步缓存更新 ============
        
        /// <summary>
        /// 异步更新自己的缓存数据
        /// 由GlobalAsyncProcessor在后台线程调用
        /// 使用activeSelf判断AI对象是否活跃，避免计算不活跃的对象
        /// </summary>
        public void UpdateOwnCacheAsync()
        {
            // 检查目标对象是否活跃
            // 对于AI敌人，当距离玩家过远时，activeSelf会被设置为false以优化性能
            // 使用activeSelf而不是activeInHierarchy，只检查对象自身的激活状态
            if (target == null || characterPoi == null || !IsTargetActiveSelf())
                return;
            
            try
            {
                // 计算新缓存数据
                var worldPos = target.transform.position;
                var newCache = new PoiCache
                {
                    MapPosition = CalculateMapPosition(worldPos),
                    RotationAngle = CalculateRotationAngle(),
                    ScaleFactor = characterPoi.ScaleFactor
                };
                
                // 更新当前缓存（原子操作）
                _currentCache = newCache;
                _lastAsyncUpdateTime = DateTime.UtcNow.Ticks;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniMap] [{name}] 更新缓存失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 判断目标对象是否活跃（使用activeSelf）
        /// 专为AI对象优化：距离远的敌人activeSelf = false，不进行计算
        /// </summary>
        private bool IsTargetActiveSelf() => target != null && target.gameObject.activeSelf;
        
        /// <summary>
        /// 判断是否需要异步更新
        /// 基于上次更新时间和30FPS频率
        /// 使用activeSelf判断AI对象是否活跃
        /// </summary>
        public bool ShouldUpdateAsync()
        {
            // 检查目标对象是否活跃
            // 对于AI敌人，当距离玩家过远时，activeSelf会被设置为false
            if (!IsTargetActiveSelf() || characterPoi == null)
                return false;
            
            // 检查是否需要更新（30FPS，33ms间隔）
            long currentTime = DateTime.UtcNow.Ticks;
            long timeSinceLastUpdate = currentTime - _lastAsyncUpdateTime;
            
            return timeSinceLastUpdate > 333333; // 33.33ms in ticks
        }
        
        /// <summary>
        /// 获取缓存数据
        /// 主线程调用，用于渲染
        /// 完全信任异步处理器，不进行有效性检查
        /// </summary>
        public void GetCachedData(out Vector2 mapPosition, out float rotationAngle, out float scale)
        {
            mapPosition = _currentCache.MapPosition;
            rotationAngle = _currentCache.RotationAngle;
            scale = _currentCache.ScaleFactor;
        }
        
        /// <summary>
        /// 初始化缓存
        /// 在对象启用时或设置时调用
        /// </summary>
        private void InitializeCache()
        {
            if (target == null || characterPoi == null) return;
            
            var worldPos = target.transform.position;
            _currentCache = new PoiCache
            {
                MapPosition = CalculateMapPosition(worldPos),
                RotationAngle = CalculateRotationAngle(),
                ScaleFactor = characterPoi.ScaleFactor
            };
            
            _lastAsyncUpdateTime = DateTime.UtcNow.Ticks;
        }
        
        // ============ 位置刷新方法 ============
        
        /// <summary>
        /// 刷新POI位置
        /// 使用缓存数据计算并更新UI显示位置
        /// </summary>
        private void RefreshPosition()
        {
            try
            {
                if (minimapEntry == null || rectTransform == null)
                    return;
                    
                // 使用缓存位置
                Vector3 position = minimapEntry.transform.localToWorldMatrix.MultiplyPoint(
                    new Vector3(_currentCache.MapPosition.x, _currentCache.MapPosition.y, 0)
                );
                rectTransform.position = position;
                
                // 更新缩放和旋转
                UpdateScale();
                UpdateRotation();
            }
            catch 
            {
                // 静默失败，避免影响游戏运行
            }
        }

        // ============ Update循环 ============
        
        /// <summary>
        /// Unity Update循环
        /// 主线程调用，完全信任并使用缓存数据更新UI
        /// 使用activeInHierarchy判断UI对象是否在层级中激活
        /// </summary>
        private void Update()
        {
            // 快速跳过非激活对象（UI层级检查）
            if (!gameObject.activeInHierarchy) return;
            
            // 检查目标对象是否活跃（AI对象检查）
            // 如果目标不活跃（如距离远的敌人），也不更新UI
            if (!IsTargetActiveSelf()) return;
            
            // 直接从缓存获取数据并更新
            GetCachedData(out var mapPosition, out var rotationAngle, out var scale);
            UpdateFromCache(mapPosition, rotationAngle, scale);
        }
        
        /// <summary>
        /// 使用缓存数据更新UI
        /// 完全信任缓存数据的有效性
        /// </summary>
        private void UpdateFromCache(Vector2 mapPosition, float rotationAngle, float scale)
        {
            // 更新位置
            if (minimapEntry != null && rectTransform != null)
            {
                try
                {
                    Vector3 position = minimapEntry.transform.localToWorldMatrix.MultiplyPoint(
                        new Vector3(mapPosition.x, mapPosition.y, 0)
                    );
                    rectTransform.position = position;
                }
                catch 
                {
                    // 静默失败
                }
            }
            
            // 更新旋转（仅方向图标）
            if (characterPoi is DirectionPointOfInterest)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);
            }
            else
            {
                transform.rotation = Quaternion.identity;
            }
            
            // 更新缩放
            UpdateScaleFromCache(scale);
        }
        
        /// <summary>
        /// 使用缓存缩放更新UI缩放
        /// </summary>
        private void UpdateScaleFromCache(float scale)
        {
            if (iconContainer == null) return;
            
            iconContainer.localScale = Vector3.one * scale / ParentLocalScale;
            
            if (characterPoi != null && characterPoi.IsArea && areaDisplay != null)
            {
                areaDisplay.BorderWidth = areaLineThickness / ParentLocalScale;
                areaDisplay.FalloffDistance = 1f / ParentLocalScale;
            }
        }

        // ============ 原有的更新方法 ============
        
        /// <summary>
        /// 更新缩放
        /// </summary>
        private void UpdateScale()
        {
            float num = ((characterPoi != null) ? characterPoi.ScaleFactor : 1f);
            iconContainer.localScale = Vector3.one * num / ParentLocalScale;
            if (characterPoi != null && characterPoi.IsArea)
            {
                areaDisplay.BorderWidth = areaLineThickness / ParentLocalScale;
                areaDisplay.FalloffDistance = 1f / ParentLocalScale;
            }
        }

        /// <summary>
        /// 更新位置
        /// </summary>
        private void UpdatePosition()
        {
            if (!IsTargetActiveSelf()) return;
                
            if (cachedWorldPosition != target.transform.position)
            {
                RefreshPosition();
            }
            else
            {
                // 位置未变，使用缓存
                if (minimapEntry != null && rectTransform != null)
                {
                    try
                    {
                        Vector3 position = minimapEntry.transform.localToWorldMatrix.MultiplyPoint(
                            new Vector3(_currentCache.MapPosition.x, _currentCache.MapPosition.y, 0)
                        );
                        rectTransform.position = position;
                    }
                    catch 
                    {
                        // 静默失败
                    }
                }
            }
        }

        /// <summary>
        /// 更新旋转
        /// </summary>
        private void UpdateRotation()
        {
            base.transform.rotation = Quaternion.identity;
        }
        
        // ============ 生命周期方法 ============
        
        /// <summary>
        /// 对象启用时调用
        /// 注册到异步处理器并初始化缓存
        /// </summary>
        private void OnEnable()
        {
            // 注册到全局异步处理器
            GlobalAsyncProcessor.Register(this);
            
            // 初始化自己的缓存
            InitializeCache();
        }
        
        /// <summary>
        /// 对象禁用时调用
        /// 从异步处理器注销并清理缓存
        /// </summary>
        private void OnDisable()
        {
            // 从全局异步处理器注销
            GlobalAsyncProcessor.Unregister(this);
            
            // 清理缓存
            _currentCache = default;
            _lastAsyncUpdateTime = 0;
        }
        
        // ============ 静态方法（场景中心点） ============
        
        /// <summary>
        /// 设置当前场景中心点
        /// 在场景加载完成时调用，避免每次计算都使用反射
        /// </summary>
        public static async UniTask SetCurrentSceneCenter(SceneLoadingContext context)
        {
            if (string.IsNullOrEmpty(context.sceneName)) return;
            
            // 直接使用公共属性获取场景中心点
            centerOfObjectScene = await GetSceneCenterFromSettingsAsync(context.sceneName);
        }
        
        /// <summary>
        /// 从MiniMapSettings获取场景中心点
        /// 使用公共属性，避免反射调用
		/// 异步方法
        /// </summary>
		private static async UniTask<Vector3> GetSceneCenterFromSettingsAsync(string sceneID)
		{
			// 方法1: 使用UniTask.Run在后台线程获取
			return await UniTask.Run(() =>
			{
				var settings = MiniMapSettings.Instance;
				if (settings == null) return Vector3.zero;
				
				foreach (var mapEntry in settings.maps)
				{
					if (mapEntry.sceneID == sceneID)
					{
						return mapEntry.mapWorldCenter;
					}
				}
				
				return settings.combinedCenter;
			});
		}
    }
}