#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Contract resolver for populating the long-lived state model (<c>CodecStatus</c>,
    /// <c>CodecConfiguration</c>) straight from each codec document (#129, stage 6d).
    /// </summary>
    /// <remarks>
    /// <para>Newtonsoft's default populate appends every list item to the existing list, so a model
    /// fed every document grows without limit; that is why the plugin stopped populating the
    /// long-lived model, and why feedbacks that read it never changed. This resolver makes the
    /// model safe to populate repeatedly:</para>
    /// <list type="bullet">
    /// <item>a list whose items have an <c>id</c> is merged by id (<see cref="IdKeyedListConverter{T}"/>):
    /// partial feedback updates one item and a ghost removes one;</item>
    /// <item>any other list is replaced by the one in the document;</item>
    /// <item>excluded properties (lists other code maintains itself) are left alone.</item>
    /// </list>
    /// <para>Lists use <see cref="ObjectCreationHandling.Replace"/>, so the property's setter runs
    /// after every populate (the converter still merges into the existing list). Setters that keep a
    /// count, such as <c>Networks</c> → <c>NetworkCount</c>, stay in step.</para>
    /// <para>Contracts are cached per instance; use one instance for the life of the program, and
    /// <see cref="Warm"/> it off the receive path. Has no Crestron dependencies.</para>
    /// </remarks>
    public sealed class StateModelContractResolver : DefaultContractResolver
    {
        private readonly HashSet<string> _excluded;

        /// <param name="excluded">Properties not to populate, as (declaring type, CLR property name).</param>
        public StateModelContractResolver(IEnumerable<(Type DeclaringType, string Property)>? excluded = null)
        {
            _excluded = new HashSet<string>(
                (excluded ?? Enumerable.Empty<(Type, string)>()).Select(e => Key(e.Item1, e.Item2)),
                StringComparer.Ordinal);
        }

        /// <summary>Serializer settings that use this resolver, for <see cref="CodecJson.PopulateFromToken"/>.</summary>
        public JsonSerializerSettings Settings => new JsonSerializerSettings { ContractResolver = this };

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (member.DeclaringType != null && _excluded.Contains(Key(member.DeclaringType, member.Name)))
            {
                property.Ignored = true;
                return property;
            }

            var itemType = ListItemType(property.PropertyType);
            if (itemType == null)
                return property;

            property.ObjectCreationHandling = ObjectCreationHandling.Replace;
            if (property.Converter == null && itemType.IsClass && itemType != typeof(string) && HasId(itemType))
                property.Converter = (JsonConverter)Activator.CreateInstance(
                    typeof(IdKeyedListConverter<>).MakeGenericType(itemType))!;

            return property;
        }

        /// <summary>
        /// Builds the contracts and property accessors for <typeparamref name="T"/>'s whole tree, by
        /// writing out a new instance and populating another from it, so the first codec document
        /// isn't slowed by that one-off work. Best effort: failures are returned, not thrown.
        /// </summary>
        public Exception? Warm<T>() where T : new()
        {
            try
            {
                var serializer = JsonSerializer.Create(Settings);
                var sample = JObject.FromObject(new T(), serializer);
                using (var reader = sample.CreateReader())
                    serializer.Populate(reader, new T());
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        private static string Key(Type declaringType, string property) => declaringType.FullName + "." + property;

        private static Type? ListItemType(Type? type)
        {
            // Arrays keep Newtonsoft's default (a new array each time); the model uses List<T>.
            if (type == null || type == typeof(string) || type.IsArray)
                return null;
            foreach (var i in type.IsInterface ? type.GetInterfaces().Append(type) : type.GetInterfaces())
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))
                    return i.GetGenericArguments()[0];
            return null;
        }

        // The same rule IdKeyedListConverter uses to find the id: a property whose JSON name is "id",
        // matched without regard to case.
        private static bool HasId(Type itemType) =>
            itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p =>
                string.Equals(p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? p.Name, "id",
                    StringComparison.OrdinalIgnoreCase));
    }
}
