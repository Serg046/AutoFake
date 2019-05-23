using System;
using System.Reflection;

namespace AutoFake
{
    internal static class ReflectionUtils
    {
        public static object Invoke(Assembly assembly, MethodInfo method, params object[] parameters)
        {
            var delegateType = assembly.GetType(method.DeclaringType.FullName, true);
            var delegateInfo = delegateType.GetMethod(method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var instance = Activator.CreateInstance(delegateType);
            return delegateInfo.Invoke(instance, parameters);
        }
    }
}
