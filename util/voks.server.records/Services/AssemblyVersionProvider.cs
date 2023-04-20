using System.Diagnostics;
using System.Reflection;

namespace voks.server.records;

public static class AssemblyVersionProvider
{
    public static bool IsDebugAssembly()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var debuggableAttributes = assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>();
        var checkIsDebugAssembly = debuggableAttributes.Any(da => da.IsJITTrackingEnabled);
        return checkIsDebugAssembly;
    }
}
