using System.Reflection;

namespace Oxygen.Patches
{
    internal static class Util
    {
        public static T GetPrivateField<T>(this object obj, string field)
        {
            return (T)obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }
    }
}
