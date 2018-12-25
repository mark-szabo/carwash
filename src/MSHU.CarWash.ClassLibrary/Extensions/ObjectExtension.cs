using Newtonsoft.Json;

namespace MSHU.CarWash.ClassLibrary.Extensions
{
    public static class ObjectExtension
    {
        public static string ToJson(this object o)
        {
            return JsonConvert.SerializeObject(o);
        }
    }
}
