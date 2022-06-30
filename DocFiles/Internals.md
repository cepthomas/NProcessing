
# Internals

A place to store notes that don't fit anywhere else, like inside my head.


- VS 2019 solution, targets .NET6 WinForms.
- Uses Roslyn for in-memory compilation.
- No installer yet, it's a build-it-yerself for now. Eventually a nuget package might be created.
- Settings and log are in `C:\Users\<user>\AppData\Local\Ephemera\NProcessing`.


## Graphics Processing
- The app is all WinForms so I started with the builtin GDI+. Some weirdness/slowness started happening so I moved to
[SkiaSharp](https://github.com/mono/SkiaSharp) which is a major improvement. I also tried OpenTK but it is a
bit of a pain to set up consistently and sensitive to the GPU and OpenGL drivers installed.
- [This graphics tool](http://kynosarges.org/WpfPerformance.html) was useful too.
- Note that if you create an app with SkiaSharp, be sure to uncheck the Build config box "Prefer 32 bit".

## Windows Key Handling
How windows handles key presses. For example Shift+A produces:
- KeyDown: KeyCode=Keys.ShiftKey, KeyData=Keys.ShiftKey | Keys.Shift, Modifiers=Keys.Shift
- KeyDown: KeyCode=Keys.A, KeyData=Keys.A | Keys.Shift, Modifiers=Keys.Shift
- KeyPress: KeyChar='A'
- KeyUp: KeyCode=Keys.A
- KeyUp: KeyCode=Keys.ShiftKey
Also note that Windows steals TAB, RETURN, ESC, and arrow keys so they are not currently implemented.

## Windows Mouse Handling
From (https://docs.microsoft.com/en-us/dotnet/framework/winforms/mouse-events-in-windows-forms).

Mouse events occur in the following order:
- MouseEnter
- MouseMove
- MouseHover / MouseDown / MouseWheel
- MouseUp
- MouseLeave

MouseDown sequence looks like this:
- MouseDown
- Click
- MouseClick
- MouseUp

## Visual Studio
- VS2017 occasionally exhibits some bizarre behavior doing debug. Per the interwebs, disabling the Diagnostic Tools option in the debug settings appears to help.
- Disable these warnings: IDE1006, CS1591.
- After some update, these appeared:
  `warning MSB3277: Found conflicts between different versions of "System.IO.Compression" that could not be resolved.`
  Commenting out the redirect in the .proj file fixed that... shouldn't have.
  
# Third Party

This application uses these FOSS components:
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).
- [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks/blob/main/README.md)
- [NBagOfUis](https://github.com/cepthomas/NBagOfUis/blob/main/README.md)
