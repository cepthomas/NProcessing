
This section describes the general structure and syntax rules of script files.  

Script functions are very C#-like because essentially it is C# (without OO requirements). The compiler adds in the surrounding boilerplate and compiles the whole mess in memory where it executes.  

Any functions specified by [Processing Functions](ScriptApiProcessing) are written here.  

# General
Double slash `//` is used for comments. C style `/* ... */` is not supported.  

Names cannot have spaces or begin with a number.  

# Script Classes
Classes are supported (see utils.np) with one caveat. Because of the design of C# (unlike java/Processing), some hoops had to be jumped through to allow defined classes access to script properties and functions. It's not ideal but (for now at least) you need to use a qualification `s.`:
```c#
class klass
{
    public void DoIt(int val)
    {
        s.print("DoIt got:", val);
    }
```

# Include
A simple include mechanism is supplied in lieu of the Processing `import`. It's pretty much an insert-whole-file-body-here, no wildcard support. Tries absolute path first, then relative to current. It needs to be specified before contents are referenced. If one were to get grandiose, a true import could be implemented.
```c#
#import "utils.np"
```
