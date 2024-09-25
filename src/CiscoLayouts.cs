using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PepperDash.Core;
using PepperDash.Core.Intersystem;
using PepperDash.Core.Intersystem.Tokens;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace epi_videoCodec_ciscoExtended.V2
{
    /*
     *  *s Video Layout CurrentLayouts AvailableLayouts 1 LayoutName: "Grid"
        *s Video Layout CurrentLayouts AvailableLayouts 2 LayoutName: "Stack"
        *s Video Layout CurrentLayouts AvailableLayouts 3 LayoutName: "Prominent"
        *s Video Layout CurrentLayouts AvailableLayouts 4 LayoutName: "Focus"
        ** end
     *
     *  *s Video Layout CurrentLayouts AvailableLayouts 1 (ghost=True):
        *s Video Layout CurrentLayouts AvailableLayouts 2 (ghost=True):
        *s Video Layout CurrentLayouts AvailableLayouts 3 (ghost=True):
        *s Video Layout CurrentLayouts AvailableLayouts 4 (ghost=True):
        ** end
     *
     * *s Video Layout CurrentLayouts ActiveLayout: "Stack"
       ** end
     */
    public class CiscoLayouts : CiscoRoomOsFeature, IHasEventSubscriptions, IHandlesResponses, IHasPolls, IHasCodecLayoutsAvailable
    {
        private readonly CiscoRoomOsDevice parent;

        private string activeLayout = "";
        private Dictionary<int, string> currentLayouts = new Dictionary<int, string>();
 
        public CiscoLayouts(CiscoRoomOsDevice parent)
            : base(parent.Key + "-Layouts")
        {
            this.parent = parent;
            Subscriptions = new List<string>
            {
                "Status/Video/Layout/CurrentLayouts/ActiveLayout",
                "Status/Video/Layout/CurrentLayouts/AvailableLayouts/LayoutName"   
            };

            Polls = new List<string>
            {
                "xStatus Video Layout CurrentLayouts AvailableLayouts LayoutName",
                "xStatus Video Layout CurrentLayouts ActiveLayout"                
            };

            LocalLayoutFeedback = new StringFeedback("Key" + "-CurrentLayout", () => activeLayout);
            AvailableLayoutsFeedback = new StringFeedback(() => UpdateLayoutsXSig(AvailableLayouts));

            LocalLayoutFeedback.OutputChange += (sender, args) =>
            {
                var handler = CurrentLayoutChanged;
                if (handler != null)
                {
                    handler(this, new CurrentLayoutChangedEventArgs {CurrentLayout = args.StringValue});
                }
            };

            AvailableLayoutsFeedback.OutputChange += (sender, args) =>
            {
                var handler = AvailableLayoutsChanged;
                if (handler != null)
                {
                    handler(this, new AvailableLayoutsChangedEventArgs { AvailableLayouts = AvailableLayouts });
                }
            };

            LocalLayoutFeedback.RegisterForDebug(parent);
        }

        public IEnumerable<string> Subscriptions { get; private set; }

        public IEnumerable<string> Polls { get; private set; }

        public bool HandlesResponse(string response)
        {
            return response.StartsWith("*s Video Layout CurrentLayouts ", StringComparison.Ordinal);
        }

        public void HandleResponse(string response)
        {
            var newLayouts = new Dictionary<int, string>();
            const string layoutUpdatePattern = @"(\d+)\s+LayoutName:\s""([^""]*)""";
            const string activeLayoutPattern = @"ActiveLayout:\s""([^""]*)""";

            Debug.Console(1, this, "Parsing layout response...");

            foreach (var line in response.Split('|'))
            {
                try
                {
                    var match = Regex.Match(line, layoutUpdatePattern);
                    if (match.Success)
                    {
                        var index = match.Groups[1].Value;
                        var layoutName = match.Groups[2].Value;

                        Debug.Console(1, this, "Parsing layout update pattern... index:{0} layout:{1}", index, layoutName);
                        newLayouts.Add(Convert.ToInt32(index), layoutName);
                    }

                    match = Regex.Match(line, activeLayoutPattern);
                    if (match.Success)
                    {
                        activeLayout = match.Groups[1].Value;
                        Debug.Console(1, this, "Parsing active layout... layout:{1}", activeLayout);
                        LocalLayoutFeedback.FireUpdate();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Console(1, this, "Caught an exception parsing a layout response:{0}", ex);
                    throw;
                }
            }

            currentLayouts = newLayouts;
            LocalLayoutFeedback.FireUpdate();
        }

        public void LocalLayoutToggle()
        {
            if (string.IsNullOrEmpty(activeLayout))
            {
                var layoutToSend = AvailableLayouts.FirstOrDefault();

                if (layoutToSend != null)
                    parent.SendText(layoutToSend.Command);
            }
            else
            {
                var currentLayoutIndex = AvailableLayouts.FindIndex(m => m.Label == activeLayout);
                var nextIndex = currentLayoutIndex + 1;

                if (nextIndex > AvailableLayouts.Count)
                {
                    nextIndex = 0;
                }

                Debug.Console(1, this, "Attempting to toggle layout to index:{0}", nextIndex);

                var layoutToSend = AvailableLayouts.ElementAtOrDefault(nextIndex);
                if (layoutToSend != null)
                    parent.SendText(layoutToSend.Command);
            }
        }

        public void LocalLayoutToggleSingleProminent()
        {
            if (activeLayout != "Prominent")
            {
                var layoutToSend = AvailableLayouts.FirstOrDefault(l => l.Label.Equals("Prominent"));
                if (layoutToSend != null)
                    parent.SendText(layoutToSend.Command);
            }
            else
            {
                var layoutToSend = AvailableLayouts.FirstOrDefault(l => l.Label.Equals("Grid"));
                if (layoutToSend != null)
                    parent.SendText(layoutToSend.Command);
            }
        }

        public void MinMaxLayoutToggle()
        {

        }

        public StringFeedback LocalLayoutFeedback { get; private set; }

        public event EventHandler<AvailableLayoutsChangedEventArgs> AvailableLayoutsChanged;

        public event EventHandler<CurrentLayoutChangedEventArgs> CurrentLayoutChanged;

        public StringFeedback AvailableLayoutsFeedback { get; private set; }

        public List<CodecCommandWithLabel> AvailableLayouts
        {
            get
            {
                return currentLayouts.Values.Select(l => new CodecCommandWithLabel("xCommand Video Layout SetLayout LayoutName: " + l, l)).ToList();
            }
        }

        public void LayoutSet(string layout)
        {
            var layoutToSet =
                AvailableLayouts.FirstOrDefault(x => x.Label.Equals(layout, StringComparison.OrdinalIgnoreCase));

            if (layout != null)
            {
                parent.SendText(layoutToSet.Command);
            }
        }

        public void LayoutSet(CodecCommandWithLabel layout)
        {
            parent.SendText(layout.Command);
        }

        private static string UpdateLayoutsXSig(List<CodecCommandWithLabel> layoutList)
        {
            var layouts = layoutList;
            var layoutIndex = 1;
            var tokenArray = new XSigToken[layouts.Count];

            if (layouts.Count == 0)
            {
                var clearBytes = XSigHelpers.ClearOutputs();
                return Encoding.GetEncoding(Utils.XSigEncoding).GetString(clearBytes, 0, clearBytes.Length);
            }

            foreach (var layout in layouts)
            {
                var arrayIndex = layoutIndex - 1;
                tokenArray[arrayIndex] = new XSigSerialToken(layoutIndex, layout.Label);
                layoutIndex++;
            }

            return Utils.GetXSigString(tokenArray);
        }
    }
}