#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Merges a RoomOS list (calls, cameras, connectors, ...) into an existing list by item <c>id</c>
    /// (#129, stage 5).
    /// </summary>
    /// <remarks>
    /// <para>RoomOS feedback sends only the list items that changed, each with its <c>id</c>; a removed
    /// item arrives as <c>{"ghost": "True", "id": "3"}</c>. Newtonsoft's default populate appends every
    /// item to an existing list instead, which duplicates entries and never removes any. With this
    /// converter on a list property, populating:</para>
    /// <list type="bullet">
    /// <item>updates the existing item with the same <c>id</c> in place (fields not sent are kept),</item>
    /// <item>adds an item with a new <c>id</c> at the end,</item>
    /// <item>removes the item a ghost names, and</item>
    /// <item>keeps every item the message didn't mention.</item>
    /// </list>
    /// <para>The item's <c>id</c> is its property whose JSON name is <c>id</c> (as the contract resolver
    /// sees it, so <c>[JsonProperty("id")]</c> works), read as a string. A single object instead of an
    /// array is treated as a list of one, as RoomOS sends for one-item lists. Items without an
    /// <c>id</c> are added. Has no Crestron dependencies.</para>
    /// </remarks>
    public sealed class IdKeyedListConverter<T> : JsonConverter where T : class
    {
        public override bool CanConvert(Type objectType) => typeof(IList<T>).IsAssignableFrom(objectType);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var list = existingValue as IList<T> ?? CreateList(objectType);
            if (reader.TokenType == JsonToken.Null)
                return list;

            var token = JToken.Load(reader);
            var items = token is JArray array ? array.Children<JObject>() : token is JObject single ? new[] { single } : Enumerable.Empty<JObject>();
            var idProperty = IdProperty(serializer);

            foreach (var item in items)
            {
                var id = item["id"]?.ToString();
                var existing = id == null || idProperty == null ? null : list.FirstOrDefault(x => IdOf(x, idProperty) == id);

                if (IsGhost(item))
                {
                    if (existing != null)
                        list.Remove(existing);
                    continue;
                }

                if (existing != null)
                {
                    using (var itemReader = item.CreateReader())
                        serializer.Populate(itemReader, existing);
                    continue;
                }

                var created = item.ToObject<T>(serializer);
                if (created != null)
                    list.Add(created);
            }

            return list;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            if (value is IEnumerable<T> items)
                foreach (var item in items)
                    serializer.Serialize(writer, item);
            writer.WriteEndArray();
        }

        private static IList<T> CreateList(Type objectType) =>
            objectType.IsInterface || objectType.IsAbstract
                ? new List<T>()
                : (IList<T>)Activator.CreateInstance(objectType)!;

        private static JsonProperty? IdProperty(JsonSerializer serializer) =>
            serializer.ContractResolver.ResolveContract(typeof(T)) is JsonObjectContract contract
                ? contract.Properties.GetClosestMatchProperty("id")
                : null;

        private static string? IdOf(T item, JsonProperty idProperty)
        {
            var value = idProperty.ValueProvider?.GetValue(item);
            return value switch
            {
                null => null,
                string s => s,
                _ => value.GetType().GetProperty("Value")?.GetValue(value)?.ToString() ?? value.ToString(),
            };
        }

        private static bool IsGhost(JObject item) =>
            string.Equals(item["ghost"]?.ToString(), "True", StringComparison.OrdinalIgnoreCase);
    }
}
