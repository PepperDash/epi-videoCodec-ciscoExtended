using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Devices.Common.Codec;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Contains data classes for managing bookings and meeting information from Cisco codecs.
    /// This class provides the structure for parsing and handling booking data from the codec's API.
    /// </summary>
    public class CiscoExtendedCodecBookings
    {
        /// <summary>
        /// Represents the total number of booking rows returned from the codec.
        /// </summary>
        public class TotalRows
        {
            /// <summary>
            /// Gets or sets the total row count as a string value.
            /// </summary>
            public string Value { get; set; }
        }

        /// <summary>
        /// Contains result information including total row count for booking queries.
        /// </summary>
        public class ResultInfo
        {
            /// <summary>
            /// Gets or sets the total rows information.
            /// </summary>
            public TotalRows TotalRows { get; set; }

            /// <summary>
            /// Initializes a new instance of the ResultInfo class.
            /// </summary>
            public ResultInfo()
            {
                TotalRows = new TotalRows();
            }
        }

        /// <summary>
        /// Represents the last updated timestamp for booking information.
        /// </summary>
        public class LastUpdated
        {
            string _value;

            /// <summary>
            /// Gets or sets the last updated datetime value.
            /// Automatically converts between string and DateTime representations.
            /// </summary>
            public DateTime Value
            {
                get
                {
                    try
                    {
                        return DateTime.Parse(_value);
                    }
                    catch
                    {
                        return new DateTime();
                    }
                }
                set
                {
                    _value = value.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        public class Id
        {
            public string Value { get; set; }
        }

        public class Title
        {
            public string Value { get; set; }
        }

        public class Agenda
        {
            public string Value { get; set; }
        }

        public class Privacy
        {
            public string Value { get; set; }
        }

        public class FirstName
        {
            public string Value { get; set; }
        }

        public class LastName
        {
            public string Value { get; set; }
        }

        public class Email
        {
            public string Value { get; set; }
        }


        public class Organizer
        {
            public FirstName FirstName { get; set; }
            public LastName LastName { get; set; }
            public Email Email { get; set; }
            [JsonProperty("CiscoCallId")]
            public Id OrganizerId { get; set; }

            public Organizer()
            {
                FirstName = new FirstName();
                LastName = new LastName();
                Email = new Email();
            }
        }

        public class StartTime
        {
            public DateTime Value { get; set; }
        }

        public class StartTimeBuffer
        {
            public string Value { get; set; }
        }

        public class EndTime
        {
            public DateTime Value { get; set; }
        }

        public class EndTimeBuffer
        {
            public string Value { get; set; }
        }

        public class Time
        {
            public StartTime StartTime { get; set; }
            public StartTimeBuffer StartTimeBuffer { get; set; }
            public EndTime EndTime { get; set; }
            public EndTimeBuffer EndTimeBuffer { get; set; }

            public Time()
            {
                StartTime = new StartTime();
                EndTime = new EndTime();
            }
        }

        public class MaximumMeetingExtension
        {
            public string Value { get; set; }
        }

        public class MeetingExtensionAvailability
        {
            public string Value { get; set; }
        }

        public class BookingStatus
        {
            public string Value { get; set; }
        }

        public class BookingStatusMessage
        {
            public string Value { get; set; }
        }

        public class Enabled
        {
            public string Value { get; set; }
        }

        public class Url
        {
            public string Value { get; set; }
        }

        public class MeetingNumber
        {
            public string Value { get; set; }
        }

        public class Password
        {
            public string Value { get; set; }
        }

        public class HostKey
        {
            public string Value { get; set; }
        }

        public class DialInNumbers
        {
        }

        public class Webex
        {
            public Enabled Enabled { get; set; }
            public Url Url { get; set; }
            public MeetingNumber MeetingNumber { get; set; }
            public Password Password { get; set; }
            public HostKey HostKey { get; set; }
            public DialInNumbers DialInNumbers { get; set; }
        }

        public class Encryption
        {
            public string Value { get; set; }
        }

        public class Role
        {
            public string Value { get; set; }
        }

        public class Recording
        {
            public string Value { get; set; }
        }

        public class Number
        {
            public string Value { get; set; }
        }

        public class Protocol
        {
            public string Value { get; set; }
        }

        public class CallRate
        {
            public string Value { get; set; }
        }

        public class CallType
        {
            public string Value { get; set; }
        }

        public class CiscoCall
        {
            [JsonProperty("CiscoCallId")]
            public string CiscoCallId { get; set; }
            public Number Number { get; set; }
            public Protocol Protocol { get; set; }
            public CallRate CallRate { get; set; }
            public CallType CallType { get; set; }
        }

        public class Calls
        {
            public List<CiscoCall> Call { get; set; }

            public Calls()
            {
                Call = new List<CiscoCall>();
            }
        }

        public class ConnectMode
        {
            public string Value { get; set; }
        }

        public class DialInfo
        {
            public Calls Calls { get; set; }
            public ConnectMode ConnectMode { get; set; }

            public DialInfo()
            {
                Calls = new Calls();
                ConnectMode = new ConnectMode();
            }
        }

        public class Booking
        {
            [JsonProperty("id")]
            public string StringId { get; set; }
            [JsonProperty("Id")]
            public Id Id { get; set; }
            public Title Title { get; set; }
            public Agenda Agenda { get; set; }
            public Privacy Privacy { get; set; }
            public Organizer Organizer { get; set; }
            public Time Time { get; set; }
            public MaximumMeetingExtension MaximumMeetingExtension { get; set; }
            public MeetingExtensionAvailability MeetingExtensionAvailability { get; set; }
            public BookingStatus BookingStatus { get; set; }
            public BookingStatusMessage BookingStatusMessage { get; set; }
            public Webex Webex { get; set; }
            public Encryption Encryption { get; set; }
            public Role Role { get; set; }
            public Recording Recording { get; set; }
            public DialInfo DialInfo { get; set; }

            public Booking()
            {
                Time = new Time();
                Id = new Id();
                Organizer = new Organizer();
                Title = new Title();
                Agenda = new Agenda();
                Privacy = new Privacy();
                DialInfo = new DialInfo();
            }
        }

        public class BookingsListResult
        {
            [JsonProperty("Status")]
            public string Status { get; set; }
            public ResultInfo ResultInfo { get; set; }
            //public LastUpdated LastUpdated { get; set; }
            [JsonProperty("Booking")]
            public List<Booking> BookingsListResultBooking { get; set; }

            public BookingsListResult()
            {
                BookingsListResultBooking = new List<Booking>();
                ResultInfo = new ResultInfo();
            }
        }

        public class CommandResponse
        {
            public BookingsListResult BookingsListResult { get; set; }
        }

        public class RootObject
        {
            public CommandResponse CommandResponse { get; set; }
        }

        /// <summary>
        /// Extracts the necessary meeting values from the Cisco bookings response and converts them to the generic class
        /// </summary>
        /// <param name="bookings"></param>
        /// <param name="joinableCooldownSeconds">How many seconds must be between now and booked time to allow the meeting to be joined</param>
        /// <returns></returns>
        public static List<Meeting> GetGenericMeetingsFromBookingResult(List<Booking> bookings, int joinableCooldownSeconds)
        {
            var meetings = new List<Meeting>();

            if (bookings == null || bookings.Count == 0)
            {
                return meetings;
            }

            foreach (var b in bookings)
            {
                var meeting = new Meeting(joinableCooldownSeconds);

                if (b.Time == null) continue;
                
                meeting.StartTime = b.Time.StartTime.Value;
                meeting.MinutesBeforeMeeting = Int32.Parse(b.Time.StartTimeBuffer.Value) / 60;
                meeting.EndTime = b.Time.EndTime.Value;
                
                if (meeting.EndTime <= DateTime.Now) continue;

                meeting.Id = b.Id != null ? b.Id.Value : b.StringId;

                if (b.Organizer != null)
                    meeting.Organizer = string.Format("{0}, {1}", b.Organizer.LastName.Value, b.Organizer.FirstName.Value);

                if (b.Title != null)
                    meeting.Title = b.Title.Value;

                if (b.Agenda != null)
                    meeting.Agenda = b.Agenda.Value;

                if (b.Privacy != null)
                    meeting.Privacy = CodecCallPrivacy.ConvertToDirectionEnum(b.Privacy.Value);

                //#warning Update this ConnectMode conversion after testing onsite.  Expected value is "OBTP", but in PD NYC Test scenarios, "Manual" is being returned for OBTP meetings
                if (b.DialInfo.ConnectMode != null)
                    if (b.DialInfo.ConnectMode.Value.ToLower() == "obtp" || b.DialInfo.ConnectMode.Value.ToLower() == "manual")
                        meeting.IsOneButtonToPushMeeting = true;

                if (b.DialInfo.Calls.Call != null)
                {
					meeting.Dialable = b.DialInfo.Calls.Call.Count > 0;
                    foreach (var c in b.DialInfo.Calls.Call)
                    {
                        meeting.Calls.Add(new Call
                        {
                            Number = c.Number?.Value,
                            Protocol = c.Protocol?.Value,
                            CallRate = c.CallRate?.Value,
                            CallType = c.CallType?.Value
                        });
                    }
                }


                meetings.Add(meeting);

                    Debug.Console(1, "Title: {0}, ID: {1}, Organizer: {2}, Agenda: {3}", meeting.Title, meeting.Id, meeting.Organizer, meeting.Agenda);
                    Debug.Console(1, "    Start Time: {0}, End Time: {1}, Duration: {2}", meeting.StartTime, meeting.EndTime, meeting.Duration);
                    Debug.Console(1, "    Joinable: {0}\n", meeting.Joinable);
            }

            meetings.OrderBy(m => m.StartTime);

            return meetings;
        }

    }
}