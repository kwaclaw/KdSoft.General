#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace KdSoft.NamedMessagePipe
{
    public class PropertyIgnoreContractResolver: DefaultContractResolver
    {
        readonly Dictionary<Type, HashSet<string>> _ignores;

        public PropertyIgnoreContractResolver() {
            _ignores = new Dictionary<Type, HashSet<string>>();
        }

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

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
            var property = base.CreateProperty(member, memberSerialization);

            if (IsIgnored(property.DeclaringType, property.PropertyName)) {
                property.ShouldSerialize = i => false;
                property.Ignored = true;
            }

            return property;
        }
    }
}
#endif