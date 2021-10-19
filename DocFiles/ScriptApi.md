
The following sections list the supported elements in roughly the same structure as the [Processing API](https://processing.org/reference/). Refer to that document for API specifics.  

There are lots of unimplemented functions and properties, including some of the overloaded flavors. If it's not implemented, you get either a compiler error or a runtime warning.  

Note also that a lot of these have not been properly tested. Eventually there may be a real unit tester project.


# Constants
```c#
Math: QUARTER_PI, HALF_PI, PI, TWO_PI, TAU
Mouse, keys: LEFT, UP, RIGHT, DOWN, CENTER
Keys: BACKSPACE, TAB, ENTER, RETURN, ESC, DELETE, CODED, ALT, CTRL, SHIFT
Arc styles: OPEN, CHORD, PIE
Alignment: BASELINE, TOP, BOTTOM
Drawing defs: CORNER, CORNERS, RADIUS, SQUARE, ROUND, PROJECT, MITER, BEVEL
Color mode: RGB, HSB
Cursor types: ARROW, CROSS, TEXT, WAIT, HAND, MOVE
Misc: CLOSE
```

# Classes
### color
color is implemented here as a class rather than a packed unsigned int.
```c#
color(int r, int g, int b)
color(int r, int g, int b, int alpha)
color(string hex)
color(int gray)
color(int gray, int alpha)

// Added for this application:
Color NativeColor
color(Color native)
```

### PImage
```c#
int width
int height
PImage(string fname)
Bitmap image()
color get(int x, int y)
PImage get(int x, int y, int width, int height)
void set(int x, int y, color color)
void set(int x, int y, PImage img)
void resize(int width, int height)

// Added for this application:
PImage(Bitmap bm)
```

### PFont
```c#
PFont(string name, int size)

// Added for this application:
Font NativeFont
```

# Structure
```c#
virtual void draw() 
void popStyle()
void pushStyle()
virtual void setup() 
```

# Environment
```c#
bool focused 
int frameCount 
int frameRate()
void frameRate(int num)
int height 
void noSmooth() 
void size(int w, int h)
void smooth() 
void smooth(int level) 
int width 
```

# Data
```c#
int @int(double val) 
string str(object value) 
```

### Data - String Functions
```c#
string join(string[] list, char separator)
string[] split(string value, char delim)
string[] split(string value, string delim)
string[] splitTokens(string value, string delim)
string trim(string str)
string[] trim(string[] array)
```

### Data - Array Functions
```c#
None implemented.
```

# Shape

### Shape - 2D Primitives
```c#
void arc(double x1, double y1, double x2, double y2, double angle1, double angle2, int style)
void arc(double x1, double y1, double x2, double y2, double angle1, double angle2)
void ellipse(double x1, double y1, double width, double height)
void line(double x1, double y1, double x2, double y2)
void point(int x, int y)
void quad(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
void rect(double x1, double y1, double w, double h)
void triangle(double x1, double y1, double x2, double y2, double x3, double y3)
```

### Shape - Curves
```c#
void bezier(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
void curve(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
```

### Shape - 3D Primitives
```c#
None implemented.
```

### Shape - Attributes
```c#
void strokeCap(int style)
void strokeJoin(int style)
void strokeWeight(int width) 
```

### Shape - Vertex
```c#
void beginShape()
void endShape()
void endShape(int mode)
void vertex(int x, int y)
```

### Shape - Loading & Displaying
```c#
None implemented.
```

# Input

### Input - Mouse
```c#
int mouseButton 
virtual void mouseClicked() 
virtual void mouseDragged() 
virtual void mouseMoved() 
bool mouseIsPressed 
virtual void mousePressed() 
virtual void mouseReleased() 
virtual void mouseWheel() 
int mouseWheelValue 
int mouseX 
int mouseY 
int pMouseX 
int pMouseY 
```

### Input - Keyboard
```c#
char key 
int keyCode
virtual void keyPressed() 
virtual void keyReleased() 
virtual void keyTyped() 
bool keyTypedP
```

### Input - Files
```c#
string[] loadStrings(string filename)
```

### Input - Time & Date
```c#
int day() 
int hour() 
int millis() 
int minute() 
int month() 
int second() 
int year() 
```

# Output

### Output - Text Area
```c#
void print(params object[] args)
void printArray(params object[] what)
```

### Output - Image
```c#
None implemented.
```

### Output - Files
```c#
None implemented.
```

### Output - PrintWriter
```c#
None implemented.
```

# Transform
```c#
void popMatrix() 
void pushMatrix() 
void rotate(double angle) 
void scale(double sc) 
void scale(double scx, double scy) 
void translate(double dx, double dy) 
```

# Lights & Camera
```c#
None implemented.
```

# Color

### Color - Setting
```c#
void background(int r, int g, int b, int a)
void background(int r, int g, int b) 
void background(int gray) 
void background(int gray, int a) 
void background(color pcolor) 
void background(string pcolor) 
void background(string pcolor, int alpha) 
void background(PImage img) 
void colorMode(mode)
void fill(string scolor, int a) 
void fill(string scolor) 
void fill(color pcolor, int a) 
void fill(color pcolor) 
void fill(int gray) 
void fill(int r, int g, int b) 
void fill(int r, int g, int b, int a)
void fill(int gray, int a) 
void noFill() 
void noStroke() 
void stroke(string scolor) 
void stroke(int r, int g, int b, int a)
void stroke(int r, int g, int b) 
void stroke(int gray) 
void stroke(int gray, int a) 
void stroke(color pcolor) 
void stroke(color pcolor, int a) 
void stroke(string scolor, int a) 
```

### Color - Creating & Reading
```c#
int alpha(color color) 
int blue(color color) 
double brightness(color color) 
color color(int r, int g, int b) 
color color(int gray) 
int green(color color) 
double hue(color color) 
color lerpColor(color c1, color c2, double amt)
int red(color color) 
double saturation(color color) 
```

# Image

### Image - Loading & Displaying
```c#
void image(PImage img, double x, double y)
void image(PImage img, double x1, double y1, double x2, double y2)
PImage loadImage(string filename)
```

### Image - Textures
```c#
None implemented.
```

### Image - Pixels
```c#
None implemented.
```

# Rendering - Shaders
```c#
None implemented.
```

# Typography

### Typography - Loading & Displaying
```c#
PFont createFont(string name, int size)
void text(string s, double x, double y)
void textFont(PFont font)
```

### Typography - Attributes
```c#
void textSize(int pts) 
int textWidth(string s) 
int textWidth(char ch) 
```

### Typography - Metrics
```c#
int textAscent() 
int textDescent() 
```

# Math

### Math - Calculation
```c#
int abs(int val) 
double abs(double val) 
int ceil(double val) 
double constrain(double val, double min, double max) 
int constrain(int val, int min, int max) 
double dist(double x1, double y1, double x2, double y2) 
double dist(double x1, double y1, double z1, double x2, double y2, double z2) 
double exp(double exponent) 
int floor(double val) 
double lerp(double start, double stop, double amt) 
double log(double val) 
double mag(double x, double y, double z) 
double mag(double x, double y) 
double map(double val, double start1, double stop1, double start2, double stop2) 
double max(double[] vals) 
int max(int[] vals) 
double min(double[] vals) 
int min(int[] vals) 
double norm(double val, double start, double stop) 
double pow(double val, double exponent) 
int round(double val) 
double sq(double val) 
double sqrt(double val) 
int truncate(double val) 
```

### Math - Trigonometry
```c#
double acos(double val) 
double asin(double val) 
double atan(double val) 
double atan2(double y, double x) 
double cos(double angle) 
double degrees(double rad) 
double radians(double degrees) 
double sin(double angle) 
double tan(double angle) 
```

### Math - Random
```c#
double random(double max) 
double random(double min, double max)
int random(int max) // added
int random(int min, int max) // added
double randomGaussian() 
void randomSeed(int seed) 
```
