using Duckov.Modding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ZoinkModdingLibrary.Utils
{
    public static class ModFileOperations
    {
        private static Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();


        private static string? GetCallerAssemblyDirectory()
        {
            var stackTrace = new System.Diagnostics.StackTrace();
            var frames = stackTrace.GetFrames();

            // 跳过当前方法（0）和调用此方法的方法（1）
            for (int i = 2; i < frames.Length; i++)
            {
                var method = frames[i].GetMethod();
                var assembly = method.Module.Assembly;

                // 排除系统程序集和当前库
                if (!IsSystemOrCurrentLibrary(assembly))
                {
                    var location = assembly.Location;
                    return Path.GetDirectoryName(location);
                }
            }

            return null;
        }

        private static bool IsSystemOrCurrentLibrary(Assembly assembly)
        {
            var name = assembly.FullName;
            return name.StartsWith("System.") ||
                   name.StartsWith("Microsoft.") ||
                   name.StartsWith("mscorlib") ||
                   name.StartsWith("ZoinkModdingLibrary");
        }
        public static string? GetDirectory()
        {
            return GetCallerAssemblyDirectory();
        }

        public static Sprite? LoadSprite(string? texturePath, ModLogger? logger = null)
        {
            logger ??= ModLogger.DefultLogger;
            lock (LoadedSprites)
            {
                if (string.IsNullOrEmpty(texturePath))
                {
                    return null;
                }
                if (LoadedSprites.ContainsKey(texturePath))
                {
                    return LoadedSprites[texturePath];
                }
                string? directoryName = GetDirectory();
                if (string.IsNullOrEmpty(directoryName))
                {
                    logger.LogError("Failed to get directory for loading sprite.");
                    return null;
                }
                string path = Path.Combine(directoryName, "textures");
                string text = Path.Combine(path, texturePath);
                if (File.Exists(text))
                {
                    byte[] data = File.ReadAllBytes(text);
                    Texture2D texture2D = new Texture2D(2, 2);
                    if (texture2D.LoadImage(data))
                    {
                        Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
                        if (!LoadedSprites.ContainsKey(texturePath))
                        {
                            LoadedSprites[texturePath] = sprite;
                        }
                        return sprite;
                    }
                }
                return null;
            }
        }

        public static JObject? LoadJson(string filePath, ModLogger? logger = null)
        {
            logger ??= ModLogger.DefultLogger;
            string? directoryName = GetDirectory();
            if (string.IsNullOrEmpty(directoryName))
            {
                logger.LogError("Failed to get directory for loading json.");
                return null;
            }
            string path = Path.Combine(directoryName, "config");
            string text = Path.Combine(path, filePath);
            try
            {
                if (File.Exists(text))
                {
                    string jsonText = File.ReadAllText(text);
                    return JObject.Parse(jsonText);
                }
                return null;
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to Load Json({text}):\n{e.Message}");
                return null;
            }
        }

        public static void SaveJson(string modConfigFileName, JObject modConfig, ModLogger? logger = null)
        {
            logger ??= ModLogger.DefultLogger;
            string? directoryName = GetDirectory();
            if (directoryName == null)
            {
                logger.LogError("Failed to get directory for saving json.");
                return;
            }
            string path = Path.Combine(directoryName, "config");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string text = Path.Combine(path, modConfigFileName);
            try
            {
                File.WriteAllText(text, modConfig.ToString(Formatting.Indented));
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to Save Json: {e.Message}");
                throw;
            }
        }
    }
}
