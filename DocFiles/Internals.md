
# Internals

- Uses Roslyn for in-memory compilation.
- No installer yet, it's a build-it-yerself for now. Eventually a nuget package might be created.
- Settings and log are in `C:\Users\<user>\AppData\Local\Ephemera\NProcessing`.


## Graphics Processing
- The app is all WinForms so I started with the builtin GDI+. Some weirdness/slowness started happening so I moved to
[SkiaSharp](https://github.com/mono/SkiaSharp) which is a major improvement. I also tried OpenTK but it is a
bit of a pain to set up consistently and sensitive to the GPU and OpenGL drivers installed.
- [This graphics tool](http://kynosarges.org/WpfPerformance.html) was useful too.
- Note that if you create an app with SkiaSharp, be sure to uncheck the Build config box "Prefer 32 bit".

