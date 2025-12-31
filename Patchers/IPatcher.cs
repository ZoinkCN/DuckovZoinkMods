using System.Reflection;

namespace MiniMap.Patchers
{
    public interface IPatcher
    {
        public string Name { get; }
        public string TargetAssemblyName { get; }
        public string TargetTypeName { get; }
        public Type? TargetType { get; }
        public bool IsPatched { get; }

        public abstract bool Patch();

        public abstract void Unpatch();
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class BindingFlagsAttribute : Attribute
    {
        private BindingFlags bindingFlags;
        public BindingFlags BindingFlags => bindingFlags;
        public BindingFlagsAttribute(BindingFlags bindingFlags)
        {
            this.bindingFlags = bindingFlags;
        }
    }
}
