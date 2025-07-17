
# What It Is

- NProcessing is a partial port of [Processing](https://processing.org/) to the Windows .NET/C# world.
- Simple Processing scripts should port easily and run fine. A porting guide is included.
- Requires VS2022 and .NET8.
- Uses Roslyn for in-memory compilation.
- No installer yet, it's a build-it-yerself for now. Eventually a nuget package might be created.
- Settings and log are in `C:\Users\<user>\AppData\Local\Ephemera\NProcessing`.

## Usage
- Basically open a .np file, press compile, then run.
- Settings and log are in `C:\Users\<user>\AppData\Local\Ephemera\NProcessing`.


### Editing Scripts
- Rather than spending the effort on a built-in script editor, I figured it would be easier for you to use your favorite external text editor. The application will watch for changes you make and indicate that recompile is needed.  
- I use Sublime Text - you can associate .np files with .cs for pretty-close syntax coloring by:  
  `View -> Syntax -> Open all with current extension as C#`


### Example Script Files
See the Examples directory for material while perusing the docs. There's a bunch that were ported from the Processing world, mainly to test.

File             | Description
----             | -----------
lsys.np          | Draws L-system trees.
gol.np           | Game of life.
flocking.np      | Port of [flocking](https://processing.org/examples/flocking.html).
generative1/2.np | Ports of [some generative files](http://alpha.editor.p5js.org/generative-design/sketches).
midi.np          | How to use midi input as a graphics controller.
utils.np         | Example of a library file for simple functions.
*.npp            | Storage for dynamic stuff. This is created and managed by the application and not generally manually edited.
temp\\\*.cs      | Generated C# files which are compiled and executed.

## Graphics
- The app is all WinForms so I started with the builtin GDI+. Some weirdness/slowness started happening so I moved to
[SkiaSharp](https://github.com/mono/SkiaSharp) which is a major improvement. I also tried OpenTK but it is a
bit of a pain to set up consistently and sensitive to the GPU and OpenGL drivers installed.
- [This graphics tool](http://kynosarges.org/WpfPerformance.html) was useful too.
- Note that if you create an app with SkiaSharp, be sure to uncheck the Build config box "Prefer 32 bit".

## External Components

- Application icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).


# Writing Scripts

This section describes the general structure and syntax of script files.  

Script functions are very C#-like because essentially it is C# (without OO requirements). The compiler adds in the surrounding boilerplate and compiles the whole mess in memory where it executes.  

You can clean up your script file using [AStyle](http://astyle.sourceforge.net/).
```
AStyle --style=allman <your-file>
```

## General
Double slash `//` is used for comments. C style `/* ... */` is not supported.  

Names cannot have spaces or begin with a number.  

Classes are supported (see utils.np).
```c#
class klass
{
    public void DoIt(int val)
    {
        Print("DoIt got:", val);
    }
```


A simple include mechanism is supplied. It's pretty much an insert-whole-file-body-here, no wildcard support.
If the path is absolute, it is used directly. If not, it is relative to the current file.
It needs to be specified before contents are referenced. If one were to get grandiose, a true import could be implemented.

```c#
#:include some path\utils.np
```

## Script API

The NProcessing script API definition is specified by the source code file `ScriptApi.cs`.
The properties and functions are organized similarly to the [Processing API](https://processing.org/reference/).
Refer to that document for specifics.  

There are lots of unimplemented functions and properties, including some of the overloaded flavors.
They are listed in `ScriptApiNotImplemented.cs`. If it's not implemented, you get either a compiler error or a runtime warning.  

Note that a lot of these have not been properly tested. Eventually there may be a real unit test project.

Notable variances from Processing to support native Windows:
- `color` is implemented here as a class rather than a packed unsigned int.
  Translation is supported by `Color NativeColor` and `color(Color native)`.
- `PImage` is the container for bitmaps: `PImage(Bitmap bm)`.
- `PFont` specifies font to use: `PFont(string name, int size)`.


## Porting from Processing

It should be fairly easy to port simple Processing scripts using this guidance. It is somewhat modelled after [p5](https://github.com/processing/p5.js/wiki/Processing-transition).
- `import` is not supported. If you have multiple files, see `#:include path`.
- Change overridden functions like `void setup()` to `public override void setup()`.
- Functions with the same name as intrinsic types need `@` e.g. `@int()`.
- Processing default `float` type is not supported - change all to `double`. Change `boolean` type to `bool`.
- ArrayList<T> becomes List<T>.
- Array `length` etc becomes `Length`. Also indexing like [][] needs to be [,].
- Iterators like `for (Boid b : boids)` becomes `foreach (Boid b in boids)`.
- `mouseWheel()` is different - instead of a `MouseEvent` param there is a `mouseWheelValue` property.
- `mousePressed` and `keyPressed` properties are now `mouseIsPressed` and `keyIsPressed` because of conflict with functions of the same name.
- pixelHeight and pixelWidth are fixed at 1.
- text() doesn't support wrapping for now.
- polygon drawing is simple: open/closed, filled/unfilled, x/y vertexes.
- println() is not implemented - use print() instead.
- frameRate property is replaced by frameRate(num) to set and frameRate() to get.
- colorMode() is used only for creating colors, not accessing them.
- Object instantiations need `new` e.g. `color alive = new color(0, 200, 0);`
- Windows steals TAB, RETURN, ESC, and the arrow keys so currently these are not implemented in keyboard input handling.

