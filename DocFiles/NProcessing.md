
# Usage
- Basically open a .np file, press compile, then run.
- Settings and log are in `C:\Users\<user>\AppData\Local\Ephemera\NProcessing`.


## Editing Scripts
- Rather than spending the effort on a built-in script editor, I figured it would be easier for you to use your favorite external text editor. The application will watch for changes you make and indicate that recompile is needed.  
- I use Sublime - you can associate .np files with .cs for pretty-close syntax coloring by:  
  `View -> Syntax -> Open all with current extension as C#`


## Example Script Files
See the Examples directory for material while perusing the docs. There's a bunch that were ported from the Processing world, mainly to test.

File | Description
---- | -----------
lsys.np | Draws L-system trees.
gol.np | Game of life.
flocking.np | Port of [flocking](https://processing.org/examples/flocking.html).
generative1/2.np | Ports of [some generative files](http://alpha.editor.p5js.org/generative-design/sketches).
midi.np | How to use midi input as a graphics controller.
utils.np | Example of a library file for simple functions.
*.npp | Storage for dynamic stuff. This is created and managed by the application and not generally manually edited.
temp\\\*.cs | Generated C# files which are compiled and executed.
