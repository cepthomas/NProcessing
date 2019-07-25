using System;
using System.Collections.Generic;
using NAudio.Midi;
using NBagOfTricks;


namespace NProcessing
{
    /// <summary>
    /// Generic container for all things midi for scripter consumption.
    /// If a property is -1 that indicates invalid or not pertinent e.g a controller event doesn't have velocity.
    /// </summary>
    public class NpMidiEvent
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

    /// <summary>Midi has received something.</summary>
    public class NpMidiInputEventArgs : EventArgs
    {
        /// <summary>Received data.</summary>
        public NpMidiEvent MidiEvent { get; set; } = null;
    }

    /// <summary>Midi wants to say something.</summary>
    public class NpMidiLogEventArgs : EventArgs
    {
        /// <summary>Category types.</summary>
        public enum LogCategory { Info, Send, Recv, Error }

        /// <summary>Category.</summary>
        public LogCategory Category { get; set; } = LogCategory.Info;

        /// <summary>Text to log.</summary>
        public string Message { get; set; } = null;
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

        #region Events
        /// <summary>Handler for message arrived.</summary>
        public event EventHandler<NpMidiInputEventArgs> InputEvent;

        /// <summary>Request for logging service.</summary>
        public event EventHandler<NpMidiLogEventArgs> LogEvent;
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
                        LogMsg(NpMidiLogEventArgs.LogCategory.Error, $"Invalid midi input: {name}");
                        inited = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMsg(NpMidiLogEventArgs.LogCategory.Error, $"Init midi in failed: {ex.Message}");
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
            NpMidiEvent mevt = null;

            switch (me.CommandCode)
            {
                case MidiCommandCode.NoteOn:
                    {
                        NoteOnEvent evt = me as NoteOnEvent;
                        mevt = new NpMidiEvent() 
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
                        mevt = new NpMidiEvent() 
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
                        mevt = new NpMidiEvent() 
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
                        mevt = new NpMidiEvent() 
                        {
                            ChannelNumber = evt.Channel,
                            ControllerId = NpMidiEvent.PITCH_CONTROL,
                            ControllerValue = evt.Pitch
                        };
                    }
                    break;
            }

            if (mevt != null)
            {
                // Pass it up for handling.
                NpMidiInputEventArgs args = new NpMidiInputEventArgs() { MidiEvent = mevt };
                InputEvent?.Invoke(this, args);
                LogMsg(NpMidiLogEventArgs.LogCategory.Recv, mevt.ToString());
            }
            // else ignore??
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void NpMidiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            LogMsg(NpMidiLogEventArgs.LogCategory.Error, $"Message:0x{e.RawMessage:X8}");
        }

        /// <summary>Ask host to do something with this.</summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMsg(NpMidiLogEventArgs.LogCategory cat, string msg)
        {
            LogEvent?.Invoke(this, new NpMidiLogEventArgs() { Category = cat, Message = msg });
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

        #region Events
        /// <summary>Request for logging service.</summary>
        public event EventHandler<NpMidiLogEventArgs> LogEvent;
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
                        _mdev = new MidiOut(device);
                        inited = true;
                    }

                    if(_mdev == null)
                    {
                        LogMsg(NpMidiLogEventArgs.LogCategory.Error, $"Invalid midi output: {name}");
                        inited = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMsg(NpMidiLogEventArgs.LogCategory.Error, $"Init midi out failed: {ex.Message}");
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
        public void Send(NpMidiEvent mevt)
        {
            // Critical code section.
            lock (_lock)
            {
                if(_mdev != null)
                {
                    int msg = 0;

                    if(mevt.Velocity >= 0)
                    {
                        NoteEvent evt = new NoteEvent(0,
                            mevt.ChannelNumber,
                            mevt.Velocity > 0 ? MidiCommandCode.NoteOn : MidiCommandCode.NoteOff,
                            mevt.NoteNumber,
                            mevt.Velocity);
                        msg = evt.GetAsShortMessage();

                    }
                    else if(mevt.ControllerId == NpMidiEvent.PITCH_CONTROL)
                    {
                        PitchWheelChangeEvent pevt = new PitchWheelChangeEvent(0,
                            mevt.ChannelNumber,
                            mevt.ControllerValue);
                        msg = pevt.GetAsShortMessage();
                        
                    }
                    else if(mevt.ControllerId >= 0)
                    {
                        ControlChangeEvent nevt = new ControlChangeEvent(0,
                            mevt.ChannelNumber,
                            (MidiController)mevt.ControllerId,
                            mevt.ControllerValue);
                        msg = nevt.GetAsShortMessage();
                        
                    }
                    // else unknown???

                    if(msg != 0)
                    {
                        _mdev.Send(msg);
                        LogMsg(NpMidiLogEventArgs.LogCategory.Send, mevt.ToString());
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
                Send(new NpMidiEvent()
                {
                    ChannelNumber = i + 1,
                    ControllerId = (int)MidiController.AllNotesOff
                });
            }
        }
        #endregion

        #region Private functions
        /// <summary>Ask host to do something with this.</summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMsg(NpMidiLogEventArgs.LogCategory cat, string msg)
        {
            LogEvent?.Invoke(this, new NpMidiLogEventArgs() { Category = cat, Message = msg });
        }
        #endregion
    }



}
