using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// Populating one long-lived model from every codec document (#129, stage 6d): lists don't grow,
/// partial feedback updates one item, wired actions fire on real changes, and an action that throws
/// doesn't stop the rest of the document. The model types here mirror the shapes in xStatus.cs.
/// </summary>
[Collection("ValueProperty counters")] // ValueProperty's counters and ActionFailed are static
public class StateModelPopulateTests
{
    private sealed class Value : ValueProperty
    {
        private string? _value;
        public string? Raw => _value;

        [JsonProperty("Value")]
        public string? Set { set { _value = value; OnValueChanged(value); } }
    }

    private sealed class Network
    {
        [JsonProperty("id")] public string? NetworkId { get; set; }
        public Value Address { get; set; } = new();
        public Value Mac { get; set; } = new();
    }

    private sealed class Line { public Value Name { get; set; } = new(); }

    private sealed class Camera { [JsonProperty("id")] public string? CameraId { get; set; } }

    private sealed class Cameras
    {
        [JsonProperty("Camera")] public List<Camera> CameraList { get; set; } = new();
    }

    private sealed class Model
    {
        private List<Network> _networks = new();

        public Value Mute { get; set; } = new();

        [JsonProperty("Network")]
        public List<Network> Networks
        {
            get => _networks;
            set
            {
                if (value == null) return;
                _networks = value;
                NetworkCount = value.Count;
                NetworkSets++;
            }
        }

        [JsonIgnore] public int NetworkCount { get; private set; }
        [JsonIgnore] public int NetworkSets { get; private set; }

        public List<Line> Line { get; set; } = new();

        public Cameras Cameras { get; set; } = new();
    }

    private static readonly StateModelContractResolver Resolver =
        new(new[] { (typeof(Cameras), nameof(Cameras.CameraList)) });

    private static void Populate(Model model, string json) =>
        CodecJson.PopulateFromToken(JToken.Parse(json), model, Resolver.Settings);

    private const string FullStatus = @"{
        ""Mute"": { ""Value"": ""Off"" },
        ""Network"": [
            { ""id"": ""1"", ""Address"": { ""Value"": ""10.0.0.1"" }, ""Mac"": { ""Value"": ""aa"" } },
            { ""id"": ""2"", ""Address"": { ""Value"": ""10.0.0.2"" }, ""Mac"": { ""Value"": ""bb"" } } ],
        ""Line"": [ { ""Name"": { ""Value"": ""L1"" } }, { ""Name"": { ""Value"": ""L2"" } } ],
        ""Cameras"": { ""Camera"": [ { ""id"": ""1"" } ] } }";

    [Fact]
    public void Repeated_Full_Documents_Do_Not_Grow_Any_List()
    {
        var model = new Model();

        Populate(model, FullStatus);
        Populate(model, FullStatus);
        Populate(model, FullStatus);

        model.Networks.Should().HaveCount(2, "items with an id are merged");
        model.Line.Should().HaveCount(2, "a list without ids is replaced");
    }

    [Fact]
    public void Partial_Feedback_Updates_Only_That_Item_And_Keeps_The_Count_In_Step()
    {
        var model = new Model();
        Populate(model, FullStatus);
        var first = model.Networks[0];

        Populate(model, @"{ ""Network"": [ { ""id"": ""2"", ""Address"": { ""Value"": ""10.0.0.9"" } } ] }");

        model.Networks.Should().HaveCount(2);
        model.Networks[0].Should().BeSameAs(first, "the untouched item is kept, not rebuilt");
        model.Networks[1].Address.Raw.Should().Be("10.0.0.9");
        model.Networks[1].Mac.Raw.Should().Be("bb", "fields not sent are kept");
        model.NetworkCount.Should().Be(2);
        model.NetworkSets.Should().Be(2, "the setter runs on every populate, so counts stay in step");

        Populate(model, @"{ ""Network"": [ { ""id"": ""1"", ""ghost"": ""True"" } ] }");

        model.Networks.Select(n => n.NetworkId).Should().Equal("2");
        model.NetworkCount.Should().Be(1);
    }

    [Fact]
    public void Excluded_Lists_Are_Left_To_The_Code_That_Maintains_Them()
    {
        var model = new Model();
        var cameras = model.Cameras.CameraList;

        Populate(model, FullStatus);

        model.Cameras.CameraList.Should().BeSameAs(cameras).And.BeEmpty();
    }

    [Fact]
    public void Wired_Values_Keep_Their_Action_And_Fire_Only_On_A_Real_Change()
    {
        var model = new Model();
        var fired = new List<string?>();
        model.Mute.ValueChangedAction = () => fired.Add(model.Mute.Raw);

        Populate(model, FullStatus);
        Populate(model, FullStatus);
        Populate(model, @"{ ""Mute"": { ""Value"": ""On"" } }");
        Populate(model, @"{ ""Network"": [ { ""id"": ""1"", ""Address"": { ""Value"": ""10.0.0.5"" } } ] }");

        fired.Should().Equal("Off", "On");
    }

    [Fact]
    public void An_Action_That_Throws_Is_Reported_And_The_Rest_Of_The_Document_Is_Populated()
    {
        var model = new Model();
        model.Mute.ValueChangedAction = () => throw new InvalidOperationException("boom");
        var failures = new List<Exception>();
        void OnFailed(ValueProperty _, Exception ex) => failures.Add(ex);
        ValueProperty.ActionFailed += OnFailed;
        try
        {
            Populate(model, FullStatus);
        }
        finally
        {
            ValueProperty.ActionFailed -= OnFailed;
        }

        failures.Should().ContainSingle().Which.Message.Should().Be("boom");
        model.Networks.Should().HaveCount(2, "values after the failing one were still populated");
    }

    [Fact]
    public void Without_A_Failure_Handler_The_Exception_Propagates_As_Before()
    {
        var model = new Model();
        model.Mute.ValueChangedAction = () => throw new InvalidOperationException("boom");

        ((Action)(() => Populate(model, FullStatus))).Should().Throw<Exception>();
    }

    private sealed class ThrowsOnRead
    {
        public string Bad => throw new InvalidOperationException("getter");
    }

    [Fact]
    public void Warm_Prepares_The_Model_And_Reports_Failures_Instead_Of_Throwing()
    {
        var resolver = new StateModelContractResolver();

        resolver.Warm<Model>().Should().BeNull();
        resolver.Warm<ThrowsOnRead>().Should().NotBeNull();
    }
}
