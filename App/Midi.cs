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
    public class MidiEventArgs : EventArgs
    {
        /// <summary>Channel number.</summary>
        public int Channel { get; set; } = -1;

        /// <summary>The note number to play.</summary>%
        public int Note { get; set; } = -1;

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
    public class MidiInput
    {
        #region Fields
        /// <summary>Midi input device.</summary>
        MidiIn? _mdev = null;
        #endregion

        #region Properties
        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";
        #endregion

        #region Events
        /// <summary>Handler for message arrived.</summary>
        public event EventHandler<MidiEventArgs>? InputEvent;
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
                            _mdev.MessageReceived += MidiIn_MessageReceived;
                            _mdev.ErrorReceived += MidiIn_ErrorReceived;
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
            _mdev?.Stop();
            _mdev?.Dispose();
            _mdev = null;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Process input midi event.
        /// </summary>
        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            MidiEvent me = MidiEvent.FromRawMessage(e.RawMessage);
            MidiEventArgs? mevt = null;

            switch (me)
            {
                case NoteOnEvent evt:
                    mevt = new MidiEventArgs()
                    {
                        Channel = evt.Channel,
                        Note = evt.NoteNumber,
                        Velocity = evt.Velocity
                    };
                    break;

                case NoteEvent evt:
                    mevt = new MidiEventArgs()
                    {
                        Channel = evt.Channel,
                        Note = evt.NoteNumber,
                        Velocity = 0
                    };
                    break;

                case ControlChangeEvent evt:
                    mevt = new MidiEventArgs()
                    {
                        Channel = evt.Channel,
                        ControllerId = (int)evt.Controller,
                        ControllerValue = evt.ControllerValue
                    };
                    break;

                case PitchWheelChangeEvent evt:
                    mevt = new MidiEventArgs()
                    {
                        Channel = evt.Channel,
                        ControllerId = MidiEventArgs.PITCH_CONTROL,
                        ControllerValue = evt.Pitch
                    };
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
        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            // do something> log? $"Message:0x{e.RawMessage:X8}");
        }
        #endregion
    }
}
