using HarmonyLib;
using MiniMap.Utils;
using System.Reflection;
using UnityEngine;

namespace MiniMap.Patchers
{
    public abstract class PatcherBase : IPatcher
    {
        public static PatcherBase? Instance { get; }

        private Type? targetType = null;
        private bool isPatched = false;

        public virtual string Name => "";
        public virtual string TargetAssemblyName => "";
        public virtual string TargetTypeName => "";
        public virtual Type? TargetType => null;
        public virtual bool IsPatched => isPatched;

        public virtual bool Patch()
        {
            try
            {
                if (isPatched)
                {
                    return true;
                }
                targetType = TargetType ?? AssemblyControl.FindTypeInAssemblies(TargetAssemblyName, TargetTypeName);
                if (targetType == null)
                {
                    Debug.LogWarning($"[{ModBehaviour.MOD_NAME}] Target Type \"{TargetTypeName}\" Not Found!");
                    return false;
                }
                Debug.Log($"[{ModBehaviour.MOD_NAME}] Patching {targetType.Name}");
                IEnumerable<MethodInfo> patchMethods = GetType().GetMethods().Where(s => s.Name.StartsWith("Patch_"));
                Dictionary<string, PatchEntry> queue = new Dictionary<string, PatchEntry>();
                Debug.Log($"[{ModBehaviour.MOD_NAME}] Find {patchMethods.Count()} Methods to patch");
                foreach (MethodInfo method in patchMethods)
                {
                    string[] splits = method.Name.Split('_');
                    if (splits.Length < 3)
                    {
                        continue;
                    }
                    string targetMethod = splits[1];
                    string patchType = splits[2];
                    BindingFlagsAttribute? attribute = method.GetCustomAttribute<BindingFlagsAttribute>();
                    MethodInfo? originalMethod = attribute == null
                        ? targetType.GetMethod(targetMethod)
                        : targetType.GetMethod(targetMethod, attribute.BindingFlags);
                    if (originalMethod == null)
                    {
                        Debug.LogWarning($"[{ModBehaviour.MOD_NAME}] Target Method \"{targetType.Name}.{targetMethod}\" Not Found!");
                        continue;
                    }
                    Debug.Log($"[{ModBehaviour.MOD_NAME}] Patching {targetType.Name}.{originalMethod.Name}");
                    PatchEntry entry;
                    if (queue.ContainsKey(originalMethod.ToString()))
                    {
                        entry = queue[originalMethod.ToString()];
                    }
                    else
                    {
                        entry = new PatchEntry(originalMethod);
                        queue.Add(originalMethod.ToString(), entry);
                    }
                    switch (patchType)
                    {
                        case "Prefix":
                            entry.prefix = new HarmonyMethod(method);
                            break;
                        case "Postfix":
                            entry.postfix = new HarmonyMethod(method);
                            break;
                        case "Transpiler":
                            entry.transpiler = new HarmonyMethod(method);
                            break;
                        case "Finalizer":
                            entry.finalizer = new HarmonyMethod(method);
                            break;
                        default:
                            Debug.LogWarning($"[{ModBehaviour.MOD_NAME}] Unknown Patch Type \"{patchType}\".");
                            break;
                    }
                }
                foreach (KeyValuePair<string, PatchEntry> item in queue)
                {
                    item.Value.Patch();
                    Debug.Log($"[{ModBehaviour.MOD_NAME}] {item.Key} Patched");
                }
                isPatched = true;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ModBehaviour.MOD_NAME}] Error When Patching: {e.Message}");
                return false;
            }
        }

        public virtual void Unpatch()
        {
            if (isPatched)
            {

            }
        }
    }
}
