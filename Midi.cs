using System;
using System.Collections.Generic;
using NAudio.Midi;
using NBagOfTricks;


namespace NProcessing
{
    /// <summary>
    /// Midi has received something.
    /// If a property is -1 that indicates invalid or not pertinent e.g a controller event doesn't have velocity.
    /// </summary>
    public class NpMidiEventArgs : EventArgs
    {
        /// <summary>Channel number.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>The note to play.</summary>
        public int NoteNumber { get; set; } = -1;

        /// <summary>The volume.</summary>
        public int Velocity { get; set; } = -1;

        /// <summary>Specific controller.</summary>
        public int ControllerId { get; set; } = -1;

        /// <summary>The controller payload.</summary>
        public int ControllerValue { get; set; } = -1;

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
        MidiIn _mdev = null;

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Properties
        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";
        #endregion

        #region Events
        /// <summary>Handler for message arrived.</summary>
        public event EventHandler<NpMidiEventArgs> InputEvent;
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
                if (_mdev != null)
                {
                    _mdev.Stop();
                    _mdev.Dispose();
                    _mdev = null;
                }

                if(name != "")
                {
                    for (int device = 0; device < MidiIn.NumberOfDevices && _mdev == null; device++)
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

                    if(_mdev == null)
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
        void NpMidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            MidiEvent me = MidiEvent.FromRawMessage(e.RawMessage);
            NpMidiEventArgs mevt = null;

            switch (me.CommandCode)
            {
                case MidiCommandCode.NoteOn:
                    {
                        NoteOnEvent evt = me as NoteOnEvent;
                        mevt = new NpMidiEventArgs() 
                        {
                            ChannelNumber = evt.Channel,
                            NoteNumber = evt.NoteNumber,
                            Velocity = evt.Velocity
                        };
                    }
                    break;

                case MidiCommandCode.NoteOff:
                    {
                        NoteEvent evt = me as NoteEvent;
                        mevt = new NpMidiEventArgs() 
                        {
                            ChannelNumber = evt.Channel,
                            NoteNumber = evt.NoteNumber,
                            Velocity = 0
                        };
                    }
                    break;

                case MidiCommandCode.ControlChange:
                    {
                        ControlChangeEvent evt = me as ControlChangeEvent;
                        mevt = new NpMidiEventArgs() 
                        {
                            ChannelNumber = evt.Channel,
                            ControllerId = (int)evt.Controller,
                            ControllerValue = evt.ControllerValue
                        };
                    }
                    break;

                case MidiCommandCode.PitchWheelChange:
                    {
                        PitchWheelChangeEvent evt = me as PitchWheelChangeEvent;
                        mevt = new NpMidiEventArgs() 
                        {
                            ChannelNumber = evt.Channel,
                            ControllerId = NpMidiEventArgs.PITCH_CONTROL,
                            ControllerValue = evt.Pitch
                        };
                    }
                    break;
            }

            if (mevt != null)
            {
                // Pass it up for handling.
                InputEvent?.Invoke(this, mevt);
            }
            // else ignore??
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void NpMidiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            // do something> log? $"Message:0x{e.RawMessage:X8}");
        }
        #endregion
    }

    /// <summary>
    /// Midi output handler.
    /// </summary>
    public class NpMidiOutput
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        MidiOut _mdev = null;

        /// <summary>Midi access synchronizer.</summary>
        object _lock = new object();

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Properties
        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";
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
                if (_mdev != null)
                {
                    _mdev.Dispose();
                    _mdev = null;
                }

                if(name != "")
                {
                    for (int device = 0; device < MidiOut.NumberOfDevices && _mdev == null; device++)
                    {
                        if (name == MidiOut.DeviceInfo(device).ProductName)
                        {
                            _mdev = new MidiOut(device);
                            inited = true;
                        }
                    }

                    if (_mdev == null)
                    {
                        ErrorInfo = $"Invalid midi output: {name}";
                        inited = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorInfo = $"Init midi out failed: {ex.Message}";
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
                _mdev?.Dispose();
                _mdev = null;

                _disposed = true;
            }
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Send a midi message.
        /// </summary>
        /// <param name="mevt"></param>
        /// <returns></returns>
        public bool Send(NpMidiEventArgs mevt)
        {
            bool ok = true;

            if(mevt.Velocity >= 0)
            {
                SendNote(mevt.ChannelNumber, mevt.NoteNumber, mevt.Velocity);
            }
            else if(mevt.ControllerId >= 0)
            {
                SendController(mevt.ChannelNumber, mevt.ControllerId, mevt.ControllerValue);
            }
            else
            {
                ok = false;
            }

            return ok;
        }

        /// <summary>
        /// Send a note midi message.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="note"></param>
        /// <param name="velocity"></param>
        public void SendNote(int channel, int note, int velocity)
        {
            if (_mdev != null)
            {
                lock (_lock)
                {
                    NoteEvent evt = new NoteEvent(0,
                        channel,
                        velocity > 0 ? MidiCommandCode.NoteOn : MidiCommandCode.NoteOff,
                        note,
                        velocity);
                    int msg = evt.GetAsShortMessage();
                    _mdev.Send(msg);
                }
            }
        }

        /// <summary>
        /// Send a controller/pitch midi message.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public void SendController(int channel, int id, int value)
        {
            if (_mdev != null)
            {
                lock (_lock)
                {
                    if (id == NpMidiEventArgs.PITCH_CONTROL)
                    {
                        PitchWheelChangeEvent pevt = new PitchWheelChangeEvent(0,
                            channel,
                            value);
                        int msg = pevt.GetAsShortMessage();
                        _mdev.Send(msg);
                    }
                    else
                    {
                        ControlChangeEvent nevt = new ControlChangeEvent(0,
                            channel,
                            (MidiController)id,
                            value);
                        int msg = nevt.GetAsShortMessage();
                        _mdev.Send(msg);
                    }
                }
            }
        }

        /// <summary>
        /// Kill them all.
        /// </summary>
        public void Kill()
        {
            for (int i = 0; i < 16; i++)
            {
                SendController(i + 1, (int)MidiController.AllNotesOff, 0);
            }
        }
        #endregion
    }
}
