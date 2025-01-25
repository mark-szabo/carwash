using System.Text.Json;

namespace CarWash.ClassLibrary.Extensions
{
    /// <summary>
    /// Extension class for Object
    /// </summary>
    public static class ObjectExtension
    {
        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="o">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string ToJson(this object o)
        {
            return JsonSerializer.Serialize(o);
        }
    }
}
