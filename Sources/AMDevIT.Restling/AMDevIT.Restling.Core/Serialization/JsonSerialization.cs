using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AMDevIT.Restling.Core.Serialization
{
    /// <summary>
    /// Implements methods to serialize and deserialize JSON data using Newtonsoft.Json or System.Text.Json 
    /// </summary>
    internal class JsonSerialization(ILogger? logger)
    {
        #region Fields

        private readonly ILogger? logger = logger;

        #endregion

        #region Properties

        protected ILogger? Logger => this.logger;

        #endregion

        #region Methods

        public string Serialize(object objectInstance)
        {
            ArgumentNullException.ThrowIfNull(objectInstance, nameof(objectInstance));

            Type objectType = objectInstance.GetType();
            return UsesNewtonsoftAttributes(objectType) ? NewtonsoftSerialize(objectInstance) : SystemTextSerialize(objectInstance);
        }

        public T? Deserialize<T>(string json)
        {
            ArgumentNullException.ThrowIfNull(json, nameof(json));
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return UsesNewtonsoftAttributes(typeof(T)) ? NewtonsoftDeserialize<T>(json) : SystemTextDeserialize<T>(json);
        }

        private string NewtonsoftSerialize(object objectInstance)
        {
            this.Logger?.LogDebug("Serializing object of type {typeName} with Newtonsoft.Json", objectInstance.GetType().FullName);

            try
            {
                return JsonConvert.SerializeObject(objectInstance);
            }
            catch(Exception exc)
            {
                throw new InvalidOperationException($"Error serializing object of type {objectInstance.GetType().FullName} with Newtonsoft.Json", exc);
            }
        }        

        private string SystemTextSerialize(object objectInstance)
        {
            this.Logger?.LogDebug("Serializing object of type {typeName} with System.Text.Json", objectInstance.GetType().FullName);

            try
            {
                return System.Text.Json.JsonSerializer.Serialize(objectInstance);
            }
            catch(Exception exc)
            {
                throw new InvalidOperationException($"Error serializing object of type {objectInstance.GetType().FullName} with System.Text.Json", exc);
            }
        }

        private T? NewtonsoftDeserialize<T>(string json)
        {
            this.Logger?.LogDebug("Deserializing object of type {typeName} with Newtonsoft.Json", typeof(T).FullName);

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

        private T? SystemTextDeserialize<T>(string json)
        {
            this.Logger?.LogDebug("Deserializing object of type {typeName} with System.Text.Json", typeof(T).FullName);

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

        private bool UsesNewtonsoftAttributes(Type type)
        {
            bool isNewtonsoft;

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

            
            bool classHasAttributes = classAttributes.Any(attr => type.GetCustomAttributes(attr, inherit: true).Length != 0);            
            bool propertyHasAttributes = type.GetProperties()
                                                      .Any(prop => propertyAttributes.Any(attr => prop.GetCustomAttributes(attr, inherit: true)
                                                                                                      .Length != 0));

            isNewtonsoft = classHasAttributes || propertyHasAttributes;
            this.Logger?.LogDebug("Type {typeName} uses Newtonsoft.Json attributes: {isNewtonsoft}", type.FullName, isNewtonsoft);
            return isNewtonsoft;
        }

        #endregion
    }
}
