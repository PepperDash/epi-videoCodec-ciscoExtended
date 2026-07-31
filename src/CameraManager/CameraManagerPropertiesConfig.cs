using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{

    /// <summary>
    /// Configuration properties for the Camera Manager. 
    /// These are used to configure the Camera Manager's behavior and link it to other devices like the network switch and room combiner.
    /// </summary>
    /// <example>
    ///   {
    ///     "key": "cameraManager1",
    ///     "name": "Camera Manager",
    ///     "type": "cameramanager",
    ///     "properties": {
    ///       "networkSwitchKey": "networkSwitch1",
    ///       "roomCombinerConfig": {
    ///         "roomCombinerKey": "roomCombiner1",
    ///         "combineScenarios": {
    ///           "divided": {
    ///             "codecConfigs": [
    ///               { "codecKey": "codecA", "cameraKeys": ["cameraA"] },
    ///               { "codecKey": "codecB", "cameraKeys": ["cameraB"] },
    ///               { "codecKey": "codecC", "cameraKeys": ["cameraC"] }
    ///             ]
    ///           },
    ///           "combined": {
    ///             "codecConfigs": [
    ///               { "codecKey": "codecB", "cameraKeys": ["cameraA", "cameraB", "cameraC"] }
    ///             ]
    ///           },
    ///           "abCombined": {
    ///             "codecConfigs": [
    ///               { "codecKey": "codecB", "cameraKeys": [{ "cameraKey": "cameraA", "cameraId": 7 }, { "cameraKey": "cameraB", "cameraId": 8 }] },
    ///               { "codecKey": "codecC", "cameraKeys": ["cameraC"] }
    ///             ]
    ///           },
    ///           "bcCombined": {
    ///             "codecConfigs": [
    ///               { "codecKey": "codecB", "cameraKeys": ["cameraB", "cameraC"] },
    ///               { "codecKey": "codecA", "cameraKeys": ["cameraA"] }
    ///             ]
    ///           }
    ///         }
    ///       }
    ///     }
    ///   }
    /// </example>
    /// <remarks>
    /// Each element in a <c>cameraKeys</c> array may be either a plain string (camera key; the
    /// camera's configured <c>defaultCameraId</c> is used) or an object
    /// <c>{ "cameraKey": "...", "cameraId": N }</c> to pin that camera to a specific slot for that
    /// scenario. The two forms may be mixed in the same array. Omitting a camera from a scenario
    /// leaves that camera unmanaged for that scenario.
    /// </remarks>
    public class CameraManagerPropertiesConfig
    {

        /// <summary>
        /// Essentials device key of the network switch that controls the cameras'
        /// PoE power and VLAN assignment. The referenced device must implement
        /// <see cref="INetworkSwitchPoeVlanManager"/>.
        /// </summary>
        [JsonProperty("networkSwitchKey")]
        public string NetworkSwitchKey { get; set; }

        /// <summary>
        /// Milliseconds to wait after issuing the camera factory reset on the source codec
        /// before tearing down PoE and moving the camera's VLAN to the target codec. The
        /// factory reset clears the camera's pairing/authentication to the source codec, but
        /// that takes time to take effect; if the camera is moved before the reset settles, it
        /// arrives at the target codec still carrying the old codec's credentials and the target
        /// reports "authentication failed, pinhole factory reset required". This delay gives the
        /// reset time to complete so the camera pairs on the first attempt. The source codec's
        /// Connected feedback is not a reliable gate (the codec keeps reporting Connected=true
        /// until the camera actually reboots), so a fixed delay is used. When omitted or
        /// non-positive, defaults to 2 000 ms.
        /// </summary>
        [JsonProperty("factoryResetSettleMs")]
        public int FactoryResetSettleMs { get; set; }

        /// <summary>
        /// When true, the manager does NOT wait a fixed <see cref="FactoryResetSettleMs"/> timer
        /// after issuing the camera factory reset before tearing down PoE and moving the camera.
        /// Instead it waits for the source codec to report the camera <b>disconnected</b> — the
        /// real signal that the factory reset took effect and the camera dropped/rebooted — then
        /// starts the PoE/VLAN cascade immediately. A safety fallback still starts the cascade if
        /// no disconnect is reported within a bounded window. Defaults to false (fixed timer).
        /// </summary>
        [JsonProperty("useCameraFactoryResetDisconnectFeedback")]
        public bool UseCameraFactoryResetDisconnectFeedback { get; set; }

        /// <summary>
        /// When true, the camera-migration cascade does NOT power-cycle (PoE off/on) the camera's
        /// network-switch port. Migrations move the camera by VLAN change and serial reassignment
        /// only, and the recovery/reconcile paths skip their PoE bounces as well. Use this when the
        /// switch port's PoE must stay on (e.g. the camera is powered/managed elsewhere, or the
        /// switch handles the power cycle itself on a VLAN change). Defaults to false, which keeps
        /// the normal PoE-off &#8594; VLAN &#8594; PoE-on cycling.
        /// </summary>
        [JsonProperty("disablePoeCycling")]
        public bool DisablePoeCycling { get; set; }

        // /// <summary>
        // /// Maximum milliseconds to wait for the camera-ghost (unpaired) event after
        // /// issuing a factory-reset command. Defaults to 30 000 ms.
        // /// </summary>
        // [JsonProperty("cameraGhostTimeoutMs")]
        // public int CameraGhostTimeoutMs { get; set; }

        // /// <summary>
        // /// Maximum milliseconds to wait for the camera to appear on the target codec
        // /// after PoE power is restored. Defaults to 60 000 ms.
        // /// </summary>
        // [JsonProperty("cameraConnectTimeoutMs")]
        // public int CameraConnectTimeoutMs { get; set; }

        // /// <summary>
        // /// Delay in milliseconds before turning on PoE power for a camera after a factory reset. Defaults to 5000 ms.
        // /// </summary>
        // [JsonProperty("cameraPoeOnDelayMs")]
        // public int CameraPoeOnDelayMs { get; set; } = 5000;

        /// <summary>
        /// Configuration for the room combiner device that the Camera Manager will use.
        /// </summary>
        [JsonProperty("roomCombinerConfig")]
        public CameraManagerRoomCombinerConfig RoomCombinerConfig { get; set; }

    }

    /// <summary>
    /// Configuration for the room combiner device that the Camera Manager will use to assign cameras to codecs in combination scenarios.
    /// </summary>
    public class CameraManagerRoomCombinerConfig
    {
        /// <summary>
        /// Key of the room combiner device
        /// </summary>
        [JsonProperty("roomCombinerKey")]
        public string RoomCombinerKey { get; set; }

        /// <summary>
        /// Dictionary of camera combine scenarios. The key is the key of a scenario in the room combiner, 
        /// and the value is an object that contains the codec key(s) and list(s) of camera keys that should be assigned to that codec in that scenario.
        /// </summary>
        public Dictionary<string, CameraManagerCombineScenarioConfig> CombineScenarios { get; set; }
    }

    /// <summary>
    /// Configuration for a codec and its assigned cameras in a specific combine scenario. 
    /// This is used by the Camera Manager to know which cameras to assign to which codecs when a combine scenario is activated.
    /// </summary>
    public class CameraManagerCombineScenarioConfig
    {
        /// <summary>
        /// Key of the codec that the cameras should be assigned to in this combine scenario
        /// </summary>
        [JsonProperty("codecConfigs")]
        public List<CameraManagerCodecConfig> CodecConfigs { get; set; }
    }

    public class CameraManagerCodecConfig
    {
        /// <summary>
        /// Key of the codec that the cameras should be assigned to in this combine scenario
        /// </summary>
        [JsonProperty("codecKey")]
        public string CodecKey { get; set; }

        /// <summary>
        /// Camera assignments for the codec in this combine scenario. Each element in the
        /// <c>cameraKeys</c> JSON array may be either a plain string (camera key, use the camera's
        /// configured <c>defaultCameraId</c>) or an object of the form
        /// <c>{ "cameraKey": "cameraA", "cameraId": 7 }</c> to pin that camera to a specific slot
        /// for this scenario. String and object forms may be mixed in the same array.
        /// </summary>
        [JsonProperty("cameraKeys")]
        [JsonConverter(typeof(CameraAssignmentListConverter))]
        public List<CameraManagerCameraAssignment> CameraAssignments { get; set; }

        /// <summary>
        /// Convenience view of the camera keys for this codec config (order preserved). Kept so
        /// existing consumers that only need the keys continue to work unchanged.
        /// </summary>
        [JsonIgnore]
        public List<string> CameraKeys
        {
            get
            {
                return CameraAssignments == null
                    ? new List<string>()
                    : CameraAssignments.Select(a => a.CameraKey).ToList();
            }
        }

        /// <summary>
        /// Returns the explicitly configured camera id (slot) for the given camera key in this
        /// scenario, or null when the camera was declared without an explicit id (string form) or
        /// is not present in this codec config. A null result means "use the camera's
        /// defaultCameraId" (today's behavior).
        /// </summary>
        /// <param name="cameraKey">Camera device key (case-insensitive).</param>
        public uint? GetConfiguredCameraId(string cameraKey)
        {
            if (CameraAssignments == null || string.IsNullOrEmpty(cameraKey))
            {
                return null;
            }

            var assignment = CameraAssignments.FirstOrDefault(a =>
                a != null && string.Equals(a.CameraKey, cameraKey, StringComparison.OrdinalIgnoreCase));
            return assignment?.CameraId;
        }

        /// <summary>
        /// Returns the explicitly configured display name for the given camera key in this scenario,
        /// or null when none was declared (string form, or object without <c>cameraName</c>). A null
        /// result means "leave the connector name unchanged".
        /// </summary>
        /// <param name="cameraKey">Camera device key (case-insensitive).</param>
        public string GetConfiguredCameraName(string cameraKey)
        {
            if (CameraAssignments == null || string.IsNullOrEmpty(cameraKey))
            {
                return null;
            }

            var assignment = CameraAssignments.FirstOrDefault(a =>
                a != null && string.Equals(a.CameraKey, cameraKey, StringComparison.OrdinalIgnoreCase));
            return assignment?.CameraName;
        }

        /// <summary>
        /// Number of camera assignments in this codec config flagged <c>primary: true</c>. Used by
        /// activation validation to reject a codec config that marks more than one primary.
        /// </summary>
        [JsonIgnore]
        public int PrimaryCount
        {
            get { return CameraAssignments == null ? 0 : CameraAssignments.Count(a => a != null && a.Primary); }
        }

        /// <summary>
        /// Returns the device key of the camera explicitly flagged <c>primary: true</c> for this
        /// codec in this scenario, or null when none is flagged. A null result means the manager
        /// does not force a main-video-source selection for this codec.
        /// </summary>
        public string GetPrimaryCameraKey()
        {
            if (CameraAssignments == null || CameraAssignments.Count == 0)
            {
                return null;
            }

            return CameraAssignments.FirstOrDefault(a => a != null && a.Primary)?.CameraKey;
        }

        /// <summary>
        /// Number of camera assignments in this codec config flagged <c>presenterTrack: true</c>.
        /// Used by activation validation to reject more than one presenter-track camera per codec.
        /// </summary>
        [JsonIgnore]
        public int PresenterTrackCount
        {
            get { return CameraAssignments == null ? 0 : CameraAssignments.Count(a => a != null && a.PresenterTrack); }
        }

        /// <summary>
        /// Returns the device key of the camera flagged <c>presenterTrack: true</c> for this codec in
        /// this scenario, or null when none is flagged. A null result means the manager leaves the
        /// codec's PresenterTrack connector untouched.
        /// </summary>
        public string GetPresenterTrackCameraKey()
        {
            if (CameraAssignments == null || CameraAssignments.Count == 0)
            {
                return null;
            }

            return CameraAssignments.FirstOrDefault(a => a != null && a.PresenterTrack)?.CameraKey;
        }
    }

    /// <summary>
    /// A single camera assignment within a codec config. Deserialized from either a plain string
    /// (camera key only) or an object <c>{ "cameraKey": "...", "cameraId": N }</c> via
    /// <see cref="CameraAssignmentConverter"/>.
    /// </summary>
    public class CameraManagerCameraAssignment
    {
        /// <summary>
        /// Key of the camera to assign to the codec in this scenario.
        /// </summary>
        [JsonProperty("cameraKey")]
        public string CameraKey { get; set; }

        /// <summary>
        /// Optional explicit camera id (slot) to pin this camera to for this scenario. When null,
        /// the camera's configured <c>defaultCameraId</c> is used.
        /// </summary>
        [JsonProperty("cameraId")]
        public uint? CameraId { get; set; }

        /// <summary>
        /// Optional display name to apply to this camera's video input connector on the target codec
        /// for this scenario (via <c>xConfiguration Video Input Connector[id] Name</c>). When null or
        /// empty, the connector name is left unchanged.
        /// </summary>
        [JsonProperty("cameraName")]
        public string CameraName { get; set; }

        /// <summary>
        /// When true, this camera is selected as the codec's main video source (the "primary" camera)
        /// once every camera for this codec is confirmed attached for the scenario. At most one
        /// assignment per codec config may set this. When none is flagged, the manager does not force
        /// a selection for that codec. Only the object form of a <c>cameraKeys</c> element can set this.
        /// </summary>
        [JsonProperty("primary")]
        public bool Primary { get; set; }

        /// <summary>
        /// When true, this camera is set as the codec's PresenterTrack camera once every camera for
        /// this codec is confirmed attached for the scenario (the manager issues
        /// <c>xConfiguration Cameras PresenterTrack Connector</c> with this camera's slot and enables
        /// PresenterTrack). At most one assignment per codec config may set this. Independent of
        /// <see cref="Primary"/>. Only the object form of a <c>cameraKeys</c> element can set this.
        /// </summary>
        [JsonProperty("presenterTrack")]
        public bool PresenterTrack { get; set; }
    }

    /// <summary>
    /// Converter for a <c>cameraKeys</c> array whose elements may be a mix of plain strings and
    /// objects. A string element becomes a <see cref="CameraManagerCameraAssignment"/> with a null
    /// <see cref="CameraManagerCameraAssignment.CameraId"/>; an object element
    /// (<c>{ "cameraKey": "...", "cameraId": N }</c>) is parsed in full. Read-only
    /// (serialization is not supported).
    /// </summary>
    public class CameraAssignmentListConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<CameraManagerCameraAssignment>);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonSerializationException(
                    $"Unexpected token {reader.TokenType} when parsing cameraKeys. Expected an array.");
            }

            var array = JArray.Load(reader);
            var result = new List<CameraManagerCameraAssignment>();
            foreach (var element in array)
            {
                switch (element.Type)
                {
                    case JTokenType.Null:
                        break;
                    case JTokenType.String:
                        result.Add(new CameraManagerCameraAssignment { CameraKey = (string)element });
                        break;
                    case JTokenType.Object:
                        var jo = (JObject)element;
                        result.Add(new CameraManagerCameraAssignment
                        {
                            CameraKey = (string)jo["cameraKey"],
                            CameraId = jo["cameraId"] != null && jo["cameraId"].Type != JTokenType.Null
                                ? (uint?)jo["cameraId"].Value<uint>()
                                : null,
                            CameraName = jo["cameraName"] != null && jo["cameraName"].Type != JTokenType.Null
                                ? (string)jo["cameraName"]
                                : null,
                            Primary = jo["primary"] != null && jo["primary"].Type != JTokenType.Null
                                && jo["primary"].Value<bool>(),
                            PresenterTrack = jo["presenterTrack"] != null && jo["presenterTrack"].Type != JTokenType.Null
                                && jo["presenterTrack"].Value<bool>()
                        });
                        break;
                    default:
                        throw new JsonSerializationException(
                            $"Unexpected token {element.Type} in cameraKeys. Expected a string or an object with 'cameraKey'.");
                }
            }
            return result;
        }

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException("CameraAssignmentListConverter is read-only.");
        }
    }
}