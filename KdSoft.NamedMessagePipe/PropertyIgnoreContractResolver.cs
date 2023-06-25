#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace KdSoft.NamedMessagePipe
{
    /// <summary>
    /// Implementation of <see cref="IContractResolver"/> that allows ignoring specific proeprties for serialization.
    /// </summary>
    /// <remarks>See <see href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm"/>.</remarks>
    public class PropertyIgnoreContractResolver: DefaultContractResolver
    {
        readonly Dictionary<Type, HashSet<string>> _ignores;

        /// <summary>Creates new instance of <see cref="PropertyIgnoreContractResolver"/>.</summary>
        public PropertyIgnoreContractResolver() {
            _ignores = new Dictionary<Type, HashSet<string>>();
        }

        /// <summary>Ignore properties for a given Type.</summary>
        public PropertyIgnoreContractResolver IgnoreProperty(Type type, params string[] jsonPropertyNames) {
            if (!_ignores.ContainsKey(type))
                _ignores[type] = new HashSet<string>();

            foreach (var prop in jsonPropertyNames)
                _ignores[type].Add(prop);

            return this;
        }

        bool IsIgnored(Type type, string jsonPropertyName) {
            var checkType = type;
            while (!_ignores.ContainsKey(checkType)) {
                checkType = checkType.BaseType;
                if (checkType == null)
                    return false;
            }

            return _ignores[checkType].Contains(jsonPropertyName);
        }

        /// <inheritdoc />
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.DeclaringType != null && property.PropertyName != null && IsIgnored(property.DeclaringType, property.PropertyName)) {
                property.ShouldSerialize = i => false;
                property.Ignored = true;
            }

            return property;
        }
    }
}
#endif