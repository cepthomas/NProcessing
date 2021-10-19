using System;
using System.Collections.Generic;
using NAudio.Midi;
using NBagOfTricks;


namespace NProcessing.App
{
    /// <summary>
    /// Midi has received something.
    /// If a property is -1 that indicates invalid or not pertinent e.g a controller event doesn't have velocity.
    /// </summary>
    public class NpMidiEventArgs : EventArgs
    {
        /// <summary>Channel number.</summary>
        public int channel { get; set; } = -1;

        /// <summary>The note number to play.</summary>%
        public int note { get; set; } = -1;

        /// <summary>The volume.</summary>
        public int velocity { get; set; } = -1;

        /// <summary>Specific controller.</summary>
        public int controllerId { get; set; } = -1;

        /// <summary>The controller payload.</summary>
        public int controllerValue { get; set; } = -1;

        /// <summary>Special id to carry pitch info.</summary>
        public const int PITCH_CONTROL = 1000;
    }

    /// <summary>
    /// Midi input handler.
    /// </summary>
    public class NpMidiInput
    {
        #region Fields
        /// <summary>Midi input device.</summary>
        MidiIn? _mdev = null;

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Properties
        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";
        #endregion

        #region Events
        /// <summary>Handler for message arrived.</summary>
        public event EventHandler<NpMidiEventArgs>? InputEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Initialize the port.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Init(string name)
        {
            bool inited = false;

            try
            {
                if (_mdev is not null)
                {
                    _mdev.Stop();
                    _mdev.Dispose();
                    _mdev = null;
                }

                if(name != "")
                {
                    for (int device = 0; device < MidiIn.NumberOfDevices && _mdev is null; device++)
                    {
                        if(name == MidiIn.DeviceInfo(device).ProductName)
                        {
                            _mdev = new MidiIn(device);
                            _mdev.MessageReceived += NpMidiIn_MessageReceived;
                            _mdev.ErrorReceived += NpMidiIn_ErrorReceived;
                            _mdev.Start();
                            inited = true;
                        }
                    }

                    if(_mdev is null)
                    {
                        ErrorInfo = $"Invalid midi input: {name}";
                        inited = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorInfo = $"Init midi in failed: {ex.Message}";
                inited = false;
            }

            return inited;
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _mdev?.Stop();
                _mdev?.Dispose();
                _mdev = null;

                _disposed = true;
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Process input midi event.
        /// </summary>
        void NpMidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            MidiEvent me = MidiEvent.FromRawMessage(e.RawMessage);
            NpMidiEventArgs? mevt = null;

            switch (me.CommandCode)
            {
                case MidiCommandCode.NoteOn:
                    {
                        NoteOnEvent evt = me as NoteOnEvent;
                        mevt = new NpMidiEventArgs() 
                        {
                            channel = evt.Channel,
                            note = evt.NoteNumber,
                            velocity = evt.Velocity
                        };
                    }
                    break;

                case MidiCommandCode.NoteOff:
                    {
                        NoteEvent evt = me as NoteEvent;
                        mevt = new NpMidiEventArgs() 
                        {
                            channel = evt.Channel,
                            note = evt.NoteNumber,
                            velocity = 0
                        };
                    }
                    break;

                case MidiCommandCode.ControlChange:
                    {
                        ControlChangeEvent evt = me as ControlChangeEvent;
                        mevt = new NpMidiEventArgs() 
                        {
                            channel = evt.Channel,
                            controllerId = (int)evt.Controller,
                            controllerValue = evt.ControllerValue
                        };
                    }
                    break;

                case MidiCommandCode.PitchWheelChange:
                    {
                        PitchWheelChangeEvent evt = me as PitchWheelChangeEvent;
                        mevt = new NpMidiEventArgs() 
                        {
                            channel = evt.Channel,
                            controllerId = NpMidiEventArgs.PITCH_CONTROL,
                            controllerValue = evt.Pitch
                        };
                    }
                    break;
            }

            if (mevt is not null)
            {
                // Pass it up for handling.
                InputEvent?.Invoke(this, mevt);
            }
            // else ignore??
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void NpMidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            // do something> log? $"Message:0x{e.RawMessage:X8}");
        }
        #endregion
    }
}
