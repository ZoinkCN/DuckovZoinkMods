using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZoinkModdingLibrary.Patcher
{
    public abstract class CompatibilityPatcherBase : PatcherBase
    {
        protected virtual List<PatcherBase>? SubPatchers { get; }

        public override PatcherBase Setup(Harmony? harmony, ModLogger? logger = null)
        {
            PatcherBase result = base.Setup(harmony, logger);
            if (SubPatchers != null)
            {
                this.logger?.Log("初始化子补丁程序...");
                foreach (var patcher in SubPatchers)
                {
                    patcher.Setup(harmony, logger);
                }
            }
            return result;
        }

        public override bool Patch()
        {
            bool result = base.Patch();
            logger?.Log($"主补丁应用完成，{result}, {SubPatchers?.Count.ToString() ?? "null"}");
            if (result && SubPatchers != null)
            {
                logger?.Log("应用子补丁程序...");
                foreach (var patcher in SubPatchers)
                {
                    patcher.Patch();
                }
            }
            return result;
        }

        public override void Unpatch()
        {
            base.Unpatch();
            if (SubPatchers != null)
            {
                logger?.Log("移除子补丁程序...");
                foreach (var patcher in SubPatchers)
                {
                    patcher.Unpatch();
                }
            }
        }
    }
}
