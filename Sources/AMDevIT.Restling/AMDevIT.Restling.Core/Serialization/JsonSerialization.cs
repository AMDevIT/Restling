using Newtonsoft.Json;

namespace AMDevIT.Restling.Core.Serialization
{
    /// <summary>
    /// Implements methods to serialize and deserialize JSON data using Newtonsoft.Json or System.Text.Json 
    /// </summary>
    internal static class JsonSerialization
    {   
        #region Methods

        public static string Serialize(object objectInstance)
        {
            ArgumentNullException.ThrowIfNull(objectInstance, nameof(objectInstance));

            Type objectType = objectInstance.GetType();
            return UsesNewtonsoftAttributes(objectType) ? NewtonsoftSerialize(objectInstance) : SystemTextSerialize(objectInstance);
        }

        public static T? Deserialize<T>(string json)
        {
            ArgumentNullException.ThrowIfNull(json, nameof(json));
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return UsesNewtonsoftAttributes(typeof(T)) ? NewtonsoftDeserialize<T>(json) : SystemTextDeserialize<T>(json);
        }

        private static string NewtonsoftSerialize(object objectInstance)
        {
            try
            {
                return JsonConvert.SerializeObject(objectInstance);
            }
            catch(Exception exc)
            {
                throw new InvalidOperationException($"Error serializing object of type {objectInstance.GetType().FullName} with Newtonsoft.Json", exc);
            }
        }        

        private static string SystemTextSerialize(object objectInstance)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(objectInstance);
            }
            catch(Exception exc)
            {
                throw new InvalidOperationException($"Error serializing object of type {objectInstance.GetType().FullName} with System.Text.Json", exc);
            }
        }

        private static T? NewtonsoftDeserialize<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch(Exception exc)
            {
                Type objectType = typeof(T);
                throw new InvalidOperationException($"Error deserializing object of type {objectType.FullName} with Newtonsoft.Json", exc);
            }
        }

        private static T? SystemTextDeserialize<T>(string json)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }
            catch(Exception exc)
            {
                Type objectType = typeof(T);
                throw new InvalidOperationException($"Error deserializing object of type {objectType.FullName} with System.Text.Json", exc);
            }
        }

        private static bool UsesNewtonsoftAttributes(Type type)
        {
            // Newtonsoft.Json attributes that can be attached to the class itself.
            Type[] classAttributes =
            [
                typeof(JsonObjectAttribute),
                typeof(JsonConverterAttribute),
                typeof(JsonExtensionDataAttribute),
                typeof(JsonIgnoreAttribute)
            ];

            // Newtonsoft.Json attributes that can be attached to the class properties.
            Type[] propertyAttributes =
            [
                typeof(JsonPropertyAttribute),
                typeof(JsonConverterAttribute),
                typeof(JsonIgnoreAttribute),
                typeof(JsonExtensionDataAttribute)
            ];

            
            bool classHasAttributes = classAttributes.Any(attr => type.GetCustomAttributes(attr, inherit: true).Any());            
            bool propertyHasAttributes = type.GetProperties()
                                                      .Any(prop => propertyAttributes.Any(attr => prop.GetCustomAttributes(attr, inherit: true)
                                                                                                      .Length != 0));

            return classHasAttributes || propertyHasAttributes;
        }

        #endregion
    }
}
