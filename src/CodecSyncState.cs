using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Core.Logging;
using Stateless;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Tracks the initial synchronization state of the codec when making a connection
    /// </summary>
    public class CodecSyncState : IKeyed
    {
        #region State Machine Definitions

        /// <summary>
        /// Represents the various states of the codec synchronization process.
        /// </summary>
        /// <remarks>
        /// The states are ordered to reflect the progression of the synchronization process.
        /// </remarks>
        public enum SyncState
        {
            Disconnected,
            Connected,
            LoginReceived,
            JsonModeSet,
            StatusReceived,
            ConfigReceived,
            SoftwareVersionReceived,
            FeedbackRegistered,
            FullySynced
        }

        /// <summary>
        /// Represents the triggers that can cause transitions between states in the codec synchronization process.
        /// </summary>
        /// <remarks>
        /// The triggers are ordered to reflect the sequence of events that can occur during synchronization.
        /// </remarks>
        public enum SyncTrigger
        {
            Connect,
            LoginMessageReceived,
            JsonResponseModeSet,
            StatusMessageReceived,
            ConfigMessageReceived,
            SoftwareVersionReceived,
            FeedbackRegistered,
            Disconnect,
            AddCommand
        }

        #endregion

        #region Private Fields

        private readonly CiscoCodec _parent;
        private StateMachine<SyncState, SyncTrigger> _stateMachine;
        private StateMachine<SyncState, SyncTrigger>.TriggerWithParameters<string> _addCommandTrigger;
        private readonly CrestronQueue<Action> _commandActions = new CrestronQueue<Action>(50);
        private readonly object _lockObject = new object();

        #endregion

        #region Public Properties

        public string Key { get; private set; }

        public event EventHandler InitialSyncCompleted;

        public bool InitialSyncComplete => _stateMachine.State == SyncState.FullySynced;

        // Backward compatibility properties
        public bool LoginMessageWasReceived => _stateMachine.State != SyncState.Disconnected && _stateMachine.State != SyncState.Connected;
        public bool JsonResponseModeSet => IsInStateOrBeyond(SyncState.JsonModeSet);
        public bool InitialStatusMessageWasReceived => IsInStateOrBeyond(SyncState.StatusReceived);
        public bool InitialConfigurationMessageWasReceived => IsInStateOrBeyond(SyncState.ConfigReceived);
        public bool InitialSoftwareVersionMessageWasReceived => IsInStateOrBeyond(SyncState.SoftwareVersionReceived);
        public bool FeedbackWasRegistered => IsInStateOrBeyond(SyncState.FeedbackRegistered);

        #endregion

        #region Constructor

        public CodecSyncState(string key, CiscoCodec parent)
        {
            Key = key;
            _parent = parent;

            // Initialize state machine
            _stateMachine = new StateMachine<SyncState, SyncTrigger>(SyncState.Disconnected);
            _addCommandTrigger = _stateMachine.SetTriggerParameters<string>(SyncTrigger.AddCommand);

            ConfigureStateMachine();

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping)
                {
                    lock (_lockObject)
                    {
                        if (_stateMachine.CanFire(SyncTrigger.Disconnect))
                        {
                            _stateMachine.Fire(SyncTrigger.Disconnect);
                        }
                    }
                }
            };
        }

        #endregion

        #region State Machine Configuration

        private void ConfigureStateMachine()
        {
            // Disconnected State
            _stateMachine.Configure(SyncState.Disconnected)
                .Permit(SyncTrigger.Connect, SyncState.Connected)
                .OnEntry(() => this.LogDebug("Codec sync state: Disconnected"));

            // Connected State
            _stateMachine.Configure(SyncState.Connected)
                .Permit(SyncTrigger.LoginMessageReceived, SyncState.LoginReceived)
                .Permit(SyncTrigger.Disconnect, SyncState.Disconnected)
                .InternalTransition(_addCommandTrigger, (command, transition) => ProcessCommand(command))
                .OnEntry(() => this.LogDebug("Codec sync state: Connected"));

            // Login Received State
            _stateMachine.Configure(SyncState.LoginReceived)
                .Permit(SyncTrigger.JsonResponseModeSet, SyncState.JsonModeSet)
                .Permit(SyncTrigger.Disconnect, SyncState.Disconnected)
                .InternalTransition(_addCommandTrigger, (command, transition) => ProcessCommand(command))
                .OnEntry(() =>
                {
                    this.LogDebug("Login Message Received.");
                    _parent.SendText("xPreferences outputmode json");
                });

            // JSON Mode Set State - This is where multiple paths can converge
            _stateMachine.Configure(SyncState.JsonModeSet)
                .Permit(SyncTrigger.StatusMessageReceived, SyncState.StatusReceived)
                .Permit(SyncTrigger.ConfigMessageReceived, SyncState.ConfigReceived)
                .Permit(SyncTrigger.SoftwareVersionReceived, SyncState.SoftwareVersionReceived)
                .Permit(SyncTrigger.Disconnect, SyncState.Disconnected)
                .InternalTransition(_addCommandTrigger, (command, transition) => ProcessCommand(command))
                .OnEntry(() => this.LogDebug("Json Response Mode Message Received."));

            // Status Received State
            _stateMachine.Configure(SyncState.StatusReceived)
                .Permit(SyncTrigger.ConfigMessageReceived, SyncState.ConfigReceived)
                .Permit(SyncTrigger.SoftwareVersionReceived, SyncState.SoftwareVersionReceived)
                .PermitIf(SyncTrigger.FeedbackRegistered, SyncState.FeedbackRegistered, () =>
                    HasReceivedAllMessages())
                .Permit(SyncTrigger.Disconnect, SyncState.Disconnected)
                .InternalTransition(_addCommandTrigger, (command, transition) => ProcessCommand(command))
                .OnEntry(() => this.LogDebug("Initial Codec Status Message Received."));

            // Config Received State
            _stateMachine.Configure(SyncState.ConfigReceived)
                .Permit(SyncTrigger.StatusMessageReceived, SyncState.StatusReceived)
                .Permit(SyncTrigger.SoftwareVersionReceived, SyncState.SoftwareVersionReceived)
                .PermitIf(SyncTrigger.FeedbackRegistered, SyncState.FeedbackRegistered, () =>
                    HasReceivedAllMessages())
                .Permit(SyncTrigger.Disconnect, SyncState.Disconnected)
                .InternalTransition(_addCommandTrigger, (command, transition) => ProcessCommand(command))
                .OnEntry(() => this.LogDebug("Initial Codec Configuration DiagnosticsMessage Received."));

            // Software Version Received State
            _stateMachine.Configure(SyncState.SoftwareVersionReceived)
                .Permit(SyncTrigger.StatusMessageReceived, SyncState.StatusReceived)
                .Permit(SyncTrigger.ConfigMessageReceived, SyncState.ConfigReceived)
                .PermitIf(SyncTrigger.FeedbackRegistered, SyncState.FeedbackRegistered, () =>
                    HasReceivedAllMessages())
                .Permit(SyncTrigger.Disconnect, SyncState.Disconnected)
                .InternalTransition(_addCommandTrigger, (command, transition) => ProcessCommand(command))
                .OnEntry(() => this.LogDebug("Initial Codec Software Information received"));

            // Feedback Registered State
            _stateMachine.Configure(SyncState.FeedbackRegistered)
                .PermitIf(SyncTrigger.StatusMessageReceived, SyncState.FullySynced, () =>
                    HasReceivedAllMessages())
                .PermitIf(SyncTrigger.ConfigMessageReceived, SyncState.FullySynced, () =>
                    HasReceivedAllMessages())
                .PermitIf(SyncTrigger.SoftwareVersionReceived, SyncState.FullySynced, () =>
                    HasReceivedAllMessages())
                .Permit(SyncTrigger.Disconnect, SyncState.Disconnected)
                .InternalTransition(_addCommandTrigger, (command, transition) => ProcessCommand(command))
                .OnEntry(() =>
                {
                    this.LogDebug("Initial Codec Feedback Registration Successful.");
                    // Check if we should transition to fully synced immediately
                    if (HasReceivedAllMessages())
                    {
                        _stateMachine.Fire(SyncTrigger.StatusMessageReceived); // Use any trigger to move to FullySynced
                    }
                });

            // Fully Synced State
            _stateMachine.Configure(SyncState.FullySynced)
                .Permit(SyncTrigger.Disconnect, SyncState.Disconnected)
                .InternalTransition(_addCommandTrigger, (command, transition) => ProcessCommand(command))
                .OnEntry(() =>
                {
                    this.LogInformation("Codec Sync Complete");
                    InitialSyncCompleted?.Invoke(this, EventArgs.Empty);
                    _parent.PollSpeakerTrack();
                    _parent.PollPresenterTrack();
                });

            // Global disconnect handling for all states
            _stateMachine.OnUnhandledTrigger((state, trigger) =>
            {
                this.LogWarning(String.Format("Unhandled trigger {0} in state {1}", trigger, state));
            });
        }

        #endregion

        #region Helper Methods

        private bool IsInStateOrBeyond(SyncState targetState)
        {
            var currentStateValue = (int)_stateMachine.State;
            var targetStateValue = (int)targetState;
            return currentStateValue >= targetStateValue;
        }

        private bool HasReceivedAllMessages()
        {
            return IsInStateOrBeyond(SyncState.StatusReceived) &&
                   IsInStateOrBeyond(SyncState.ConfigReceived) &&
                   IsInStateOrBeyond(SyncState.SoftwareVersionReceived);
        }

        private void ProcessCommand(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                if (!_commandActions.TryToEnqueue(() => _parent.SendText(query)))
                {
                    this.LogDebug("Unable to enqueue command: {command}", query);
                }
                ProcessCommandQueue();
            }
        }

        private void ProcessCommandQueue()
        {
            while (_commandActions.TryToDequeue(out Action cmd))
            {
                try
                {
                    cmd();
                }
                catch (Exception ex)
                {
                    this.LogError("Error processing user action: {message}", ex.Message);
                    this.LogVerbose(ex, "Exception");
                }
            }
        }

        #endregion

        #region Public Methods

        public void AddCommandToQueue(string query)
        {
            lock (_lockObject)
            {
                if (_stateMachine.CanFire(SyncTrigger.AddCommand))
                {
                    _stateMachine.Fire(_addCommandTrigger, query);
                }
                else
                {
                    this.LogDebug("Cannot add command in current state: {state}", _stateMachine.State);
                }
            }
        }

        public void LoginMessageReceived()
        {
            lock (_lockObject)
            {
                if (_stateMachine.CanFire(SyncTrigger.LoginMessageReceived))
                {
                    _stateMachine.Fire(SyncTrigger.LoginMessageReceived);
                }
                else
                {
                    this.LogDebug("Login message received but cannot transition from state: {state}", _stateMachine.State);
                }
            }
        }

        public void JsonResponseModeMessageReceived()
        {
            lock (_lockObject)
            {
                if (_stateMachine.CanFire(SyncTrigger.JsonResponseModeSet))
                {
                    _stateMachine.Fire(SyncTrigger.JsonResponseModeSet);
                }
                else
                {
                    this.LogDebug("JSON response mode message received but cannot transition from state: {state}", _stateMachine.State);
                }
            }
        }

        public void InitialStatusMessageReceived()
        {
            lock (_lockObject)
            {
                if (_stateMachine.CanFire(SyncTrigger.StatusMessageReceived))
                {
                    _stateMachine.Fire(SyncTrigger.StatusMessageReceived);
                }
                else
                {
                    this.LogDebug("Status message received but cannot transition from state: {state}", _stateMachine.State);
                }
            }
        }

        public void InitialConfigurationMessageReceived()
        {
            lock (_lockObject)
            {
                if (_stateMachine.CanFire(SyncTrigger.ConfigMessageReceived))
                {
                    _stateMachine.Fire(SyncTrigger.ConfigMessageReceived);
                }
                else
                {
                    this.LogDebug("Config message received but cannot transition from state: {state}", _stateMachine.State);
                }
            }
        }

        public void InitialSoftwareVersionMessageReceived()
        {
            lock (_lockObject)
            {
                if (_stateMachine.CanFire(SyncTrigger.SoftwareVersionReceived))
                {
                    _stateMachine.Fire(SyncTrigger.SoftwareVersionReceived);
                }
                else
                {
                    this.LogDebug("Software version message received but cannot transition from state: {state}", _stateMachine.State);
                }
            }
        }

        public void FeedbackRegistered()
        {
            lock (_lockObject)
            {
                if (_stateMachine.CanFire(SyncTrigger.FeedbackRegistered))
                {
                    _stateMachine.Fire(SyncTrigger.FeedbackRegistered);
                }
                else
                {
                    this.LogDebug("Feedback registered but cannot transition from state: {state}", _stateMachine.State);
                }
            }
        }

        public void CodecDisconnected()
        {
            lock (_lockObject)
            {
                if (_stateMachine.CanFire(SyncTrigger.Disconnect))
                {
                    _stateMachine.Fire(SyncTrigger.Disconnect);
                }
                else
                {
                    // If we can't fire disconnect, recreate the state machine
                    this.LogDebug("Forcing codec sync state to Disconnected");
                    ReinitializeStateMachine();
                }
            }
        }

        private void ReinitializeStateMachine()
        {
            _stateMachine = new StateMachine<SyncState, SyncTrigger>(SyncState.Disconnected);
            _addCommandTrigger = _stateMachine.SetTriggerParameters<string>(SyncTrigger.AddCommand);
            ConfigureStateMachine();
        }

        public void Connect()
        {
            lock (_lockObject)
            {
                if (_stateMachine.CanFire(SyncTrigger.Connect))
                {
                    _stateMachine.Fire(SyncTrigger.Connect);
                }
                else
                {
                    this.LogDebug("Cannot connect from state: {state}", _stateMachine.State);
                }
            }
        }

        // Diagnostic method to get current state information
        public string GetStateInfo()
        {
            return $"Current State: {_stateMachine.State}";
        }

        #endregion
    }
}