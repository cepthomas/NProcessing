# Custom for this app

The following sections list classes and functions added specifically for this application.

## Midi
Basic midi functionality has been added after enjoying the [Music Animation Machine](http://www.musanim.com).

Access midi by configuring these settings fields:
```
Midi Input: Enter a valid midi input device name.
Virtual Keyboard: If enabled a keyboard that mimics midi in is created.
```
The `About` window will show you all your midi devices.

To use midi inputs in your script, add this event handler:
```c#
virtual void midiEvent(PMidiEvent evt)
```
`PMidiEvent` contains these properties. They default to -1 if not pertinent to the message type.
```c#
int channel: Midi channel number.
int note: The note number to play.
int velocity: The volume.
int controllerId: Specific controller. There is a special value of 1000 used to indicate this carries pitch value.
int controllerValue: The controller payload.
```
