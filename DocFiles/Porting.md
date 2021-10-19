
It should be fairly easy to port simple Processing scripts using this guidance. It is somewhat modelled after [p5](https://github.com/processing/p5.js/wiki/Processing-transition).
- `import` is not supported. If you have multiple files, you can use `#import`.
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

After making these changes you can clean up your file using [AStyle](http://astyle.sourceforge.net/).
```
AStyle --style=allman <your-file>
```