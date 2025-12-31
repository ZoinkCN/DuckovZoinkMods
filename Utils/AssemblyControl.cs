using System.Reflection;
using UnityEngine;

namespace MiniMap.Utils
{
    public static class AssemblyControl
    {
        public static Type? FindTypeInAssemblies(string assembliyName, string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.FullName.Contains(assembliyName))
                {
                    Debug.Log($"[{ModBehaviour.MOD_NAME}] 找到{assembliyName}相关程序集: {assembly.FullName}");
                }

                Type type = assembly.GetType(typeName);
                if (type != null) return type;
            }

            Debug.Log($"[{ModBehaviour.MOD_NAME}] 找不到程序集");
            return null;
        }
    }
}
