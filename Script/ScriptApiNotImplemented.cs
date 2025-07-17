using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using SkiaSharp;
using Ephemera.NBagOfTricks;


// See ScriptApi.cs.

namespace NProcessing.Script
{
    public partial class ScriptCore
    {
        #region Definitions - same values as Processing
        //---- Color mode
        //public const int ARGB = 2;
        //public const int ALPHA = 4;
        #endregion

        #region Structure
        //---- Function overrides

        //---- Script functions
        protected void exit() { NotImpl(nameof(exit), "This is probably not what you want to do."); exit(); }
        //protected void popStyle() { NotImpl(nameof(popStyle)); }
        //protected void pushStyle() { NotImpl(nameof(pushStyle)); }
        //protected void thread() { NotImpl(nameof(thread)); }
        #endregion

        #region Environment 
        //---- Script properties
        //public void cursor(int which) { NotImpl(nameof(cursor)); }
        //public void cursor(PImage image) { NotImpl(nameof(cursor)); }
        //public void cursor(PImage image, int x, int y) { NotImpl(nameof(cursor)); }
        //public void delay(int msec) { NotImpl(nameof(delay)); }
        //public int displayDensity() { NotImpl(nameof(displayDensity)); }
        public void fullScreen() { NotImpl(nameof(fullScreen), "Size is set by main form."); }
        //public void noCursor() { NotImpl(nameof(noCursor)); }
        public void pixelDensity(int density) { NotImpl(nameof(pixelDensity)); }
        public int pixelHeight { get { NotImpl(nameof(pixelHeight), "Assume 1."); return 1; } }
        public int pixelWidth { get { NotImpl(nameof(pixelWidth), "Assume 1."); return 1; } }
        #endregion

        #region Data
        //public string binary(object value) { NotImpl(nameof(binary)); }
        //public bool boolean(object value) { NotImpl(nameof(boolean)); }
        //public byte @byte (object value) { NotImpl(nameof(@byte)); }
        //public char @char (object value) { NotImpl(nameof(@char)); }
        //public float @float(object value) { NotImpl(nameof(@float)); }
        //public string hex(object value) { NotImpl(nameof(hex)); }
        //public int unbinary(string value) { NotImpl(nameof(unbinary)); }
        //public int unhex(string value) { NotImpl(nameof(unhex)); }

        #region Data - String Functions
        //public string match() { NotImpl(nameof(match)); }
        //public string matchAll() { NotImpl(nameof(matchAll)); }
        //public string nf() { NotImpl(nameof(nf)); }
        //public string nfc() { NotImpl(nameof(nfc)); }
        //public string nfp() { NotImpl(nameof(nfp)); }
        //public string nfs() { NotImpl(nameof(nfs)); }
        #endregion

        #region Data - Array Functions
        //public void append() { NotImpl(nameof(append)); }
        //public void arrayCopy() { NotImpl(nameof(arrayCopy)); }
        //public void concat() { NotImpl(nameof(concat)); }
        //public void expand() { NotImpl(nameof(expand)); }
        //public void reverse() { NotImpl(nameof(reverse)); }
        //public void shorten() { NotImpl(nameof(shorten)); }
        //public void sort() { NotImpl(nameof(sort)); }
        //public void splice() { NotImpl(nameof(splice)); }
        //public void subset() { NotImpl(nameof(subset)); }
        #endregion
        #endregion

        #region Shape 
        //public void createShape() { NotImpl(nameof(createShape)); }
        //public void loadShape() { NotImpl(nameof(loadShape)); }

        #region Shape - 2D Primitives
        #endregion

        #region Shape - Curves
        public void curve(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4) { NotImpl(nameof(curve)); }
        //Draws a curved line on the screen. The first and second parameters specify the beginning control point and the last two 
        //parameters specify the ending control point. The middle parameters specify the start and stop of the curve. 
        //Longer curves can be created by putting a series of curve() functions together or using curveVertex().
        //An additional function called curveTightness() provides control for the visual quality of the curve. 
        //The curve() function is an implementation of Catmull-Rom splines.
        //The GDI function: Draws a cardinal spline through a specified array of Point structures.
        //_gr.DrawCurve(_pen, new SKPoint[4] { new SKPoint(x1, y1), new SKPoint(x2, y2), new SKPoint(x3, y3), new SKPoint(x4, y4) }, 1, 1, 0.5f);

        //public void curveDetail() { NotImpl(nameof(curveDetail)); }
        //public void curvePoint() { NotImpl(nameof(curvePoint)); }
        //public void curveTangent() { NotImpl(nameof(curveTangent)); }
        //public void curveTightness() { NotImpl(nameof(curveTightness)); }
        #endregion

        #region Shape - 3D Primitives
        //public void box() { NotImpl(nameof(box)); }
        //public void sphere() { NotImpl(nameof(sphere)); }
        //public void sphereDetail() { NotImpl(nameof(sphereDetail)); }
        #endregion

        #region Shape - Attributes
        public void ellipseMode(int mode) { NotImpl(nameof(ellipseMode), "Assume CORNER mode."); }
        public void rectMode(int mode) { NotImpl(nameof(rectMode), "Assume CORNER mode."); }
        #endregion

        #region Shape - Vertex
        //public void beginContour() { NotImpl(nameof(beginContour)); }
        //public void beginShape(int kind) { NotImpl(nameof(beginShape)); } // POINTS, LINES, TRIANGLES, TRIANGLE_FAN, TRIANGLE_STRIP, QUADS, and QUAD_STRIP
        //public void bezierVertex() { NotImpl(nameof(bezierVertex)); }
        //public void curveVertex() { NotImpl(nameof(curveVertex)); }
        //public void endContour() { NotImpl(nameof(endContour)); }
        //public void quadraticVertex() { NotImpl(nameof(quadraticVertex)); }
        #endregion

        #region Shape - Loading & Displaying
        //public void shape() { NotImpl(nameof(shape)); }
        //public void shapeMode() { NotImpl(nameof(shapeMode)); }
        #endregion
        #endregion

        #region Input
        #region Input - Mouse
        #endregion

        #region Input - Keyboard
        #endregion

        #region Input - Files
        //public void createInput() { NotImpl(nameof(createInput)); }
        //public void createReader() { NotImpl(nameof(createReader)); }
        //public void launch() { NotImpl(nameof(launch)); }
        //public void loadBytes() { NotImpl(nameof(loadBytes)); }
        //public void loadJSONArray() { NotImpl(nameof(loadJSONArray)); }
        //public void loadJSONObject() { NotImpl(nameof(loadJSONObject)); }
        //public void loadTable() { NotImpl(nameof(loadTable)); }
        //public void loadXML() { NotImpl(nameof(loadXML)); }
        //public void parseJSONArray() { NotImpl(nameof(parseJSONArray)); }
        //public void parseJSONObject() { NotImpl(nameof(parseJSONObject)); }
        //public void parseXML() { NotImpl(nameof(parseXML)); }
        //public void selectFolder() { NotImpl(nameof(selectFolder)); }
        //public void selectInput() { NotImpl(nameof(selectInput)); }
        #endregion

        #region Input - Time & Date
        #endregion
        #endregion

        #region Output
        #region Output - Text Area
        //public void println(params object[] vars) { NotImpl(nameof(print), "Use print()."); }
        #endregion

        #region Output - Image
        //public void save(string fn) { NotImpl(nameof(save)); }
        //public void saveFrame() { NotImpl(nameof(saveFrame)); }
        //public void saveFrame(string fn) { NotImpl(nameof(saveFrame)); }
        #endregion

        #region Output - Files
        //public void beginRaw() { NotImpl(nameof(beginRaw)); }
        //public void beginRecord() { NotImpl(nameof(beginRecord)); }
        //public void createOutput() { NotImpl(nameof(createOutput)); }
        //public void createWriter() { NotImpl(nameof(createWriter)); }
        //public void endRaw() { NotImpl(nameof(endRaw)); }
        //public void endRecord() { NotImpl(nameof(endRecord)); }
        #endregion

        #region Output - PrintWriter
        //public void saveBytes() { NotImpl(nameof(saveBytes)); }
        //public void saveJSONArray() { NotImpl(nameof(saveJSONArray)); }
        //public void saveJSONObject() { NotImpl(nameof(saveJSONObject)); }
        //public void saveStream() { NotImpl(nameof(saveStream)); }
        //public void saveStrings() { NotImpl(nameof(saveStrings)); }
        //public void saveTable() { NotImpl(nameof(saveTable)); }
        //public void saveXML() { NotImpl(nameof(saveXML)); }
        //public void selectOutput() { NotImpl(nameof(selectOutput)); }
        #endregion
        #endregion

        #region Transform 
        //public void applyMatrix() { NotImpl(nameof(applyMatrix)); }
        //public void printMatrix() { NotImpl(nameof(printMatrix)); }
        //public void resetMatrix() { NotImpl(nameof(resetMatrix)); }
        //public void rotateX() { NotImpl(nameof(rotateX)); }
        //public void rotateY() { NotImpl(nameof(rotateY)); }
        //public void rotateZ() { NotImpl(nameof(rotateZ)); }
        //public void shearX() { NotImpl(nameof(shearX)); }
        //public void shearY() { NotImpl(nameof(shearY)); }
        #endregion

        #region Lights & Camera
        #region Lights & Camera - Lights
        //public string ambientLight() { NotImpl(nameof(ambientLight)); }
        //public string directionalLight() { NotImpl(nameof(directionalLight)); }
        //public string lightFalloff() { NotImpl(nameof(lightFalloff)); }
        //public string lights() { NotImpl(nameof(lights)); }
        //public string lightSpecular() { NotImpl(nameof(lightSpecular)); }
        //public string noLights() { NotImpl(nameof(noLights)); }
        //public string normal() { NotImpl(nameof(normal)); }
        //public string pointLight() { NotImpl(nameof(pointLight)); }
        //public string spotLight() { NotImpl(nameof(spotLight)); }
        #endregion

        #region Lights & Camera - Camera
        //public string beginCamera() { NotImpl(nameof(beginCamera)); }
        //public string camera() { NotImpl(nameof(camera)); }
        //public string endCamera() { NotImpl(nameof(endCamera)); }
        //public string frustum() { NotImpl(nameof(frustum)); }
        //public string ortho() { NotImpl(nameof(ortho)); }
        //public string perspective() { NotImpl(nameof(perspective)); }
        //public string printCamera() { NotImpl(nameof(printCamera)); }
        //public string printProjection() { NotImpl(nameof(printProjection)); }
        #endregion

        #region Lights & Camera - Coordinates
        //public string modelX() { NotImpl(nameof(modelX)); }
        //public string modelY() { NotImpl(nameof(modelY)); }
        //public string modelZ() { NotImpl(nameof(modelZ)); }
        //public string screenX() { NotImpl(nameof(screenX)); }
        //public string screenY() { NotImpl(nameof(screenY)); }
        //public string screenZ() { NotImpl(nameof(screenZ)); }
        #endregion

        #region Lights & Camera - Material Properties
        //public string ambient() { NotImpl(nameof(ambient)); }
        //public string emissive() { NotImpl(nameof(emissive)); }
        //public string shininess() { NotImpl(nameof(shininess)); }
        //public string specular() { NotImpl(nameof(specular)); }
        #endregion
        #endregion

        #region Color
        #region Color - Setting
        //public void background(int rgb) { NotImpl(nameof(background)); }
        //public void background(int rgb, double alpha) { NotImpl(nameof(background)); }
        //public void fill(int rgb) { NotImpl(nameof(fill)); }
        //public void fill(int rgb, double alpha) { NotImpl(nameof(fill)); }
        //public void stroke(int rgb) { NotImpl(nameof(stroke)); }
        //public void stroke(int rgb, float alpha) { NotImpl(nameof(stroke)); }
        #endregion

        #region Color - Creating & Reading
        #endregion
        #endregion

        #region Image 
        //public PImage createImage(int w, int h, int format) { NotImpl(nameof(PImage)); }

        #region Image - Loading & Displaying
        public void imageMode(int mode) { NotImpl(nameof(imageMode), "Assume CORNER mode."); }
        //public void noTint() { NotImpl(nameof(noTint)); }
        //public void requestImage() { NotImpl(nameof(requestImage)); }
        //public void tint() { NotImpl(nameof(tint)); }
        #endregion

        #region Image - Textures
        //public void texture() { NotImpl(nameof(texture)); }
        //public void textureMode() { NotImpl(nameof(textureMode)); }
        //public void textureWrap() { NotImpl(nameof(textureWrap)); }
        #endregion

        #region Image - Pixels
        // Even though you may have drawn a shape with colorMode(HSB), the numbers returned will be in RGB format.
        // pixels[]
        //public void blend() { NotImpl(nameof(blend)); }
        //public void copy() { NotImpl(nameof(copy)); }
        //public void filter() { NotImpl(nameof(filter)); }
        //public PImage get(int x, int y, int w, int h) { NotImpl(nameof(get)); }
        //public color get(int x, int y) { NotImpl(nameof(get)); }
        //public void loadPixels() { NotImpl(nameof(loadPixels)); }
        //public void set(int x, int y, color pcolor) { NotImpl(nameof(set)); }
        //public void set(int x, int y, PImage src) { NotImpl(nameof(set)); }
        //public void updatePixels() { NotImpl(nameof(updatePixels)); }
        #endregion
        #endregion

        #region Rendering 
        //public void blendMode() { NotImpl(nameof(blendMode)); }
        //public void clip() { NotImpl(nameof(clip)); }
        //public void createGraphics() { NotImpl(nameof(createGraphics)); }
        //public void noClip() { NotImpl(nameof(noClip)); }

        #region Rendering - Shaders
        //public void loadShader() { NotImpl(nameof(loadShader)); }
        //public void resetShader() { NotImpl(nameof(resetShader)); }
        //public void shader() { NotImpl(nameof(shader)); }
        #endregion
        #endregion

        #region Typography
        #region Typography - Loading & Displaying
        //public PFont loadFont() { NotImpl(nameof(loadFont)); }
        #endregion

        #region Typography - Attributes
        public void textAlign(int alignX, int alignY) { NotImpl(nameof(textAlign)); }
        //public void textLeading(int leading) { NotImpl(nameof(textLeading)); }
        //public void textMode() { NotImpl(nameof(textMode)); }
        #endregion

        #region Typography - Metrics
        //public int textAscent() { return (int)Math.Round(_font.FontFamily.GetCellAscent(_font.Style) * _font.Size / _font.FontFamily.GetEmHeight(_font.Style)); }
        //public int textDescent() { return (int)Math.Round(_font.FontFamily.GetCellDescent(_font.Style) * _font.Size / _font.FontFamily.GetEmHeight(_font.Style)); }
        #endregion
        #endregion

        #region Math
        #region Math - Calculation
        #endregion

        #region Math - Trigonometry
        #endregion

        #region Math - Random
        //public void noise() { NotImpl(nameof(noise)); }
        //public void noiseDetail() { NotImpl(nameof(noiseDetail)); }
        //public void noiseSeed() { NotImpl(nameof(noiseSeed)); }
        #endregion
        #endregion
    }
}
