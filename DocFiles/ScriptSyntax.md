
# Script Syntax

This section describes the general structure and syntax rules of script files.  

Script functions are very C#-like because essentially it is C# (without OO requirements). The compiler adds in the surrounding boilerplate and compiles the whole mess in memory where it executes.  

Any functions specified by [Processing Functions](ScriptApiProcessing) are written here.  

You can clean up your script file using [AStyle](http://astyle.sourceforge.net/).
```
AStyle --style=allman <your-file>
```

## General
Double slash `//` is used for comments. C style `/* ... */` is not supported.  

Names cannot have spaces or begin with a number.  

## Script Classes
Classes are supported (see utils.np).
```c#
class klass
{
    public void DoIt(int val)
    {
        Print("DoIt got:", val);
    }
```

## Include
A simple include mechanism is supplied in lieu of the Processing `import`. It's pretty much an insert-whole-file-body-here, no wildcard support. Tries absolute path first, then relative to current. It needs to be specified before contents are referenced. If one were to get grandiose, a true import could be implemented.
```c#
Include(path\utils.np)
```
