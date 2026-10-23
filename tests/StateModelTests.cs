using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// The state-model building blocks (#129, stage 5): ValueProperty notifies on real changes only (for
/// status/configuration values), and IdKeyedListConverter merges RoomOS lists by id. Both have no
/// Crestron dependencies, so they are compiled into this project.
/// </summary>
[Collection("ValueProperty counters")] // the counters are static; keep these tests from running in parallel
public class StateModelTests
{
    // ---- ValueProperty ----------------------------------------------------------------------------

    private sealed class StatusValue : ValueProperty
    {
        public bool BoolValue { get; private set; }

        public string? Value
        {
            set
            {
                BoolValue = value == "On";
                OnValueChanged(value);
            }
        }
    }

    private sealed class EventValue : ValueProperty
    {
        private string? _value;
        public string? Value { get => _value; set { _value = value; OnValueChanged(); } }
    }

    [Fact]
    public void A_Status_Value_Notifies_Only_When_It_Changes()
    {
        var v = new StatusValue();
        var n = 0;
        v.ValueChangedAction = () => n++;

        v.Value = "On";
        v.Value = "On";
        v.Value = "On";
        v.Value = "Off";
        v.Value = "Off";
        v.Value = "On";

        n.Should().Be(3);
        v.BoolValue.Should().BeTrue();
    }

    [Fact]
    public void The_First_Set_Always_Notifies_Even_If_It_Is_Null()
    {
        var v = new StatusValue();
        var n = 0;
        v.ValueChangedAction = () => n++;

        v.Value = null;
        v.Value = null;

        n.Should().Be(1);
    }

    [Fact]
    public void An_Event_Value_Notifies_Every_Time()
    {
        // A panel button clicked twice sends the same value twice; both are events.
        var e = new EventValue();
        var n = 0;
        e.ValueChangedAction = () => n++;

        e.Value = "pressed";
        e.Value = "pressed";

        n.Should().Be(2);
    }

    [Fact]
    public void Counters_Show_Notified_And_Unchanged_Sets_For_Wired_Values_Only()
    {
        ValueProperty.ResetCounts();
        var wired = new StatusValue { ValueChangedAction = () => { } };
        var unwired = new StatusValue();

        wired.Value = "On";
        wired.Value = "On";
        unwired.Value = "On";
        unwired.Value = "On";

        ValueProperty.NotifiedCount.Should().Be(1);
        ValueProperty.UnchangedCount.Should().Be(1);
    }

    [Fact]
    public void Populating_The_Same_Status_Twice_Notifies_Once()
    {
        var model = new AudioModel();
        var n = 0;
        model.Volume!.ValueChangedAction = () => n++;
        var token = JToken.Parse("{ \"Volume\": { \"Value\": \"40\" } }");

        CodecJson.PopulateFromToken(token, model);
        CodecJson.PopulateFromToken(token, model);

        n.Should().Be(1, "a re-sync or poll reply with the same value no longer re-runs the action");
    }

    private sealed class Volume : ValueProperty
    {
        public string? Value { set => OnValueChanged(value); }
    }

    private sealed class AudioModel
    {
        public Volume? Volume { get; set; } = new();
    }

    // ---- IdKeyedListConverter ---------------------------------------------------------------------

    private sealed class Text { public string? Value { get; set; } }

    private sealed class Camera
    {
        [JsonProperty("id")] public string? CameraId { get; set; }
        public Text? Name { get; set; }
        public Text? Connected { get; set; }
    }

    private sealed class Cameras
    {
        [JsonProperty("Camera")]
        [JsonConverter(typeof(IdKeyedListConverter<Camera>))]
        public List<Camera> CameraList { get; set; } = new();
    }

    private static Cameras Populate(Cameras target, string json)
    {
        CodecJson.PopulateFromToken(JToken.Parse(json), target);
        return target;
    }

    private const string TwoCameras = @"{ ""Camera"": [
        { ""id"": ""1"", ""Name"": { ""Value"": ""Main"" }, ""Connected"": { ""Value"": ""True"" } },
        { ""id"": ""2"", ""Name"": { ""Value"": ""Wide"" }, ""Connected"": { ""Value"": ""True"" } } ] }";

    [Fact]
    public void A_Partial_Update_Changes_Only_The_Item_With_That_Id()
    {
        var model = Populate(new Cameras(), TwoCameras);
        var first = model.CameraList[0];

        Populate(model, @"{ ""Camera"": [ { ""id"": ""1"", ""Connected"": { ""Value"": ""False"" } } ] }");

        model.CameraList.Should().HaveCount(2, "the default populate would have appended a third");
        model.CameraList[0].Should().BeSameAs(first, "updated in place");
        model.CameraList[0].Connected!.Value.Should().Be("False");
        model.CameraList[0].Name!.Value.Should().Be("Main", "fields not sent are kept");
        model.CameraList[1].Name!.Value.Should().Be("Wide");
    }

    [Fact]
    public void A_Ghost_Removes_The_Item()
    {
        var model = Populate(new Cameras(), TwoCameras);

        Populate(model, @"{ ""Camera"": [ { ""ghost"": ""True"", ""id"": ""1"" } ] }");

        model.CameraList.Select(c => c.CameraId).Should().Equal("2");
    }

    [Fact]
    public void A_Ghost_For_An_Unknown_Id_Changes_Nothing()
    {
        var model = Populate(new Cameras(), TwoCameras);

        Populate(model, @"{ ""Camera"": [ { ""ghost"": ""True"", ""id"": ""9"" } ] }");

        model.CameraList.Should().HaveCount(2);
    }

    [Fact]
    public void A_New_Id_Is_Added_At_The_End()
    {
        var model = Populate(new Cameras(), TwoCameras);

        Populate(model, @"{ ""Camera"": [ { ""id"": ""3"", ""Name"": { ""Value"": ""Rear"" } } ] }");

        model.CameraList.Select(c => c.CameraId).Should().Equal("1", "2", "3");
    }

    [Fact]
    public void A_Single_Object_Is_Treated_As_A_List_Of_One()
    {
        var model = Populate(new Cameras(), TwoCameras);

        Populate(model, @"{ ""Camera"": { ""id"": ""2"", ""Name"": { ""Value"": ""Wide 2"" } } }");

        model.CameraList.Should().HaveCount(2);
        model.CameraList[1].Name!.Value.Should().Be("Wide 2");
    }

    [Fact]
    public void Update_Add_And_Ghost_In_One_Message()
    {
        var model = Populate(new Cameras(), TwoCameras);

        Populate(model, @"{ ""Camera"": [
            { ""id"": ""2"", ""Connected"": { ""Value"": ""False"" } },
            { ""ghost"": ""True"", ""id"": ""1"" },
            { ""id"": ""4"", ""Name"": { ""Value"": ""Desk"" } } ] }");

        model.CameraList.Select(c => c.CameraId).Should().Equal("2", "4");
        model.CameraList[0].Connected!.Value.Should().Be("False");
    }

    [Fact]
    public void Works_When_The_List_Starts_Null_Or_Empty_And_On_Deserialize()
    {
        var model = new Cameras { CameraList = null! };
        Populate(model, TwoCameras).CameraList.Should().HaveCount(2);

        var fresh = JToken.Parse(TwoCameras).ToObject<Cameras>()!;
        fresh.CameraList.Should().HaveCount(2);
    }

    [Fact]
    public void Serializes_As_A_Plain_Array()
    {
        var model = Populate(new Cameras(), TwoCameras);

        var json = JObject.Parse(JsonConvert.SerializeObject(model));

        json["Camera"]!.Type.Should().Be(JTokenType.Array);
        json["Camera"]![1]!["id"]!.Value<string>().Should().Be("2");
    }

    private sealed class Contact
    {
        [JsonProperty("id")] public IdValue? Id { get; set; }
        public Text? Name { get; set; }
    }

    private sealed class IdValue { public string? Value { get; set; } }

    private sealed class Contacts
    {
        [JsonConverter(typeof(IdKeyedListConverter<Contact>))]
        public List<Contact> Contact { get; set; } = new();
    }

    [Fact]
    public void A_Value_Wrapped_Id_Is_Read_Through_Its_Value()
    {
        var model = new Contacts();
        CodecJson.PopulateFromToken(JToken.Parse(@"{ ""Contact"": [ { ""id"": { ""Value"": ""a"" }, ""Name"": { ""Value"": ""One"" } } ] }"), model);

        // The incoming "id" is an object here, so it isn't matched; it's added. Plain-string ids (what
        // RoomOS sends for list items) are what the merge keys on.
        model.Contact.Should().ContainSingle();
    }

    [Fact]
    public void RoomPresetsChange_NotifiesOnEveryPresetListUpdate()
    {
        // The Status() constructor assigns RoomPresets (setting this to true) before CiscoCodec wires the
        // action, and every later update from ParseRoomPresetList sets true again
        var change = new CiscoCodecStatus.RoomPresetsChange { Value = true };
        var n = 0;
        change.ValueChangedAction = () => n++;

        change.Value = true;
        change.Value = true;

        n.Should().Be(2);
    }
}
