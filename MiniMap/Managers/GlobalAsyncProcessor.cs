using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Duckov.MiniMaps.UI;
using UnityEngine;

namespace MiniMap.Managers
{
    /// <summary>
    /// 全局异步处理器
    /// 负责以30FPS的频率更新所有CharacterPoiEntry的位置计算
    /// 将位置计算从主线程移到后台线程，提升性能
    /// 使用activeSelf判断AI对象是否活跃，避免计算不活跃的对象
    /// </summary>
    public static class GlobalAsyncProcessor
    {
        // 状态标志
        private static bool _isInitialized = false;
        private static CancellationTokenSource? _cts;
        
        // 存储所有POI条目的集合
        private static readonly HashSet<CharacterPoiEntry> _allEntries = new();
        private static readonly object _entriesLock = new object();
        
        /// <summary>
        /// 初始化异步处理器
        /// 启动30FPS的异步更新循环
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            
            _cts = new CancellationTokenSource();
            _isInitialized = true;
            
            // 启动30FPS异步循环
            ProcessAllEntriesAsync().Forget();
            
            Debug.Log("[MiniMap] GlobalAsyncProcessor 初始化完成");
        }
        
        /// <summary>
        /// 销毁异步处理器
        /// 停止异步循环并清理资源
        /// </summary>
        public static void Destroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            
            lock (_entriesLock)
            {
                _allEntries.Clear();
            }
            
            _isInitialized = false;
            Debug.Log("[MiniMap] GlobalAsyncProcessor 已销毁");
        }
        
        /// <summary>
        /// 注册POI条目到异步处理器
        /// </summary>
        /// <param name="entry">要注册的POI条目</param>
        public static void Register(CharacterPoiEntry entry)
        {
            lock (_entriesLock)
            {
                _allEntries.Add(entry);
            }
        }
        
        /// <summary>
        /// 从异步处理器注销POI条目
        /// </summary>
        /// <param name="entry">要注销的POI条目</param>
        public static void Unregister(CharacterPoiEntry entry)
        {
            lock (_entriesLock)
            {
                _allEntries.Remove(entry);
            }
        }
        
        /// <summary>
        /// 异步处理循环（30FPS）
        /// 每33ms运行一次，更新所有需要更新的POI缓存
        /// </summary>
        private static async UniTaskVoid ProcessAllEntriesAsync()
        {
            var token = _cts?.Token ?? CancellationToken.None;
            
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 收集所有需要更新的POI
                    List<CharacterPoiEntry> entriesToUpdate;
                    lock (_entriesLock)
                    {
                        entriesToUpdate = new List<CharacterPoiEntry>(_allEntries.Count);
                        
                        foreach (var entry in _allEntries)
                        {
                            // 只处理需要更新的条目
                            if (entry != null && entry.ShouldUpdateAsync())
                            {
                                entriesToUpdate.Add(entry);
                            }
                        }
                    }
                    
                    // 在后台线程并行处理所有POI
                    if (entriesToUpdate.Count > 0)
                    {
                        await UniTask.SwitchToThreadPool();
                        
                        var tasks = new UniTask[entriesToUpdate.Count];
                        for (int i = 0; i < entriesToUpdate.Count; i++)
                        {
                            var entry = entriesToUpdate[i];
                            tasks[i] = UniTask.Run(() =>
                            {
                                try
                                {
                                    entry.UpdateOwnCacheAsync();
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"[MiniMap] 处理POI {entry?.name} 失败: {e.Message}");
                                }
                            });
                        }
                        
                        await UniTask.WhenAll(tasks);
                    }
                    
                    // 等待33ms（30FPS）
                    await UniTask.Delay(33, DelayType.Realtime, PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，无需处理
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniMap] ProcessAllEntriesAsync错误: {e.Message}");
            }
        }
    }
}