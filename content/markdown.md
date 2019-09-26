# Desharp - C# & VB .NET Debugging Tool

---

## What You Can Dump Or Log

- any variables
  - primitive variables and it's primitive arrays like: `char`, `int?[]` and more 
  - collections: `Lists`, `Dictionaries` or any collections (`IList`, `IDictionary`, `ICollection`, `IEnumerable`...)
  - database results: `DataSet`, `DataTable` and `DataRow` with values
  - formated instances: `DateTimeOffset`, `DateTime`, `TimeSpan`, `Guid`, `StringBuilder`
  - any custom class instances with rendered events targets, properties values and fields values
  - anonymous objects like: `new { any = "value" }`
  - `Func<>` and `Delegate` types
  - reflection objects are only displayed as type names
- exceptions
- exceptions with inner exceptions
- much more... you can try:-)

---

## Demos & Examples

- [**Console Application Demo (C#)**](https://github.com/debug-sharp/example-console-csharp), [**(VB.NET)**](https://github.com/debug-sharp/example-console-visualbasic)  
  Demo dumps and exceptions rendering into console window, logging on HDD and optional tests running.
- [**Windows Forms Application Demo (C#)**](https://github.com/debug-sharp/example-win-forms)  
  Demo dumps and exceptions rendering into text field component, into debug output window and logging on HDD.
- [**Web Basic Application Demo (C#)**](https://github.com/debug-sharp/example-web-basic)  
  Demo dumps and exceptions rendering into floating browser bar and logging on HDD.
- [**Web MVC Application Demo (C#)**](https://github.com/debug-sharp/example-web-mvc)  
  Demo dumps and exceptions rendering into floating browser bar and logging on HDD.
- [**Web Forms Application Demo (C#)**](https://github.com/debug-sharp/example-web-forms)  
  Demo dumps and exceptions rendering into floating browser bar and logging on HDD.

---

## Do Not Miss
- [**Visual Studio code snippets**](https://github.com/debug-sharp/codesnippets)  
  Download and install predefined VS snippets for most offten `Desharp` calls.
- [**Visual Studio opener**](https://github.com/debug-sharp/editor-opener)  
  Automatic Visual Studio (or any other) editor opening on specific file and line from rendered logs and exceptions.

---

## Basic Dumping & Logging Any Structuralized Variables

```cs
using Desharp;
using System.Collections.Generic;

var list = new List<int?>() { 100, 200, null };
Debug.Dump(list);  // print list by Console.WriteLine(); or append into html response as floating window
Debug.Log(list);   // store dumped list in debug.log or debug.html file on HDD
```

Dumped result for both languages:
```
[List<Int32?>[3]]
   0: 100 [Int32]
   1: 200 [Int32]
   2: null
```

## Basic Dumping & Logging Exceptions

```cs
try {
   throw new Exception("Something wrong!");
} catch (Exception e) {
   Debug.Dump(e);  // print exception by Console.WriteLine(); or append into html response as floating window
   Debug.Log(e);   // store dumped exception in exception.log or exception.html file on HDD
}
```

Dumped result for both languages:
```
System.Exception (Hash Code: 50632145):
   Message   : Something wrong!
   Time      : 2017-06-10 13:18:07:300
   Process ID: 7972
   Thread ID : 1
   File      : /Program.cs:8
   -------
       4 | namespace ExampleConsole {
       5 |     class Program {
       6 |         static void Main(string[] args) {
       7 |             try {
   ->  8 |                 throw new Exception("Something wrong!");
       9 |             } catch (Exception e) {
      10 |                 Debug.Dump(e);  // print exception by Console.WriteLine(); or append into html response as floating window
      11 |                 Debug.Log(e);   // store dumped exception in exception.log or exception.html file on HDD
      12 |             }
   -------
   Callstack: 
      ExampleConsole.Program.Main(String[] args) /Program.cs 8
```

## All Dump Methods

Dump any values to application output (in web applications into debug bar,  
in desktop applications into console or debug output window)
```cs
Desharp.Debug.Dump(params object[] args);
```

Dump exception instance to application output if output dumping is enabled. It renders:  
- exception type, exception message and exception hash id
- yes/no if exception has been caught or not caught
- error file where exception has been thrown
- thread call stack
All inner exceptions after this exception in the same way.
```cs
Desharp.Debug.Dump(Exception exception = null, DumpOptions? options = default(DumpOptions?));
```

Dump any values to application output (in web applications into debug bar,  
in desktop applications into console or debug output window)  
This method dumps only single object with dump options like to dump  
different object depth as usual, different string length or to dump source location and more...
```cs
Desharp.Debug.Dump(object obj, DumpOptions? options = default(DumpOptions?));
```

Dump any type value to direct application output (not into web request debug  
bar in web applications!) and stop request/thread (in web applications dump into  
direct response body, in desktop applications into console or debug output window).
```cs
Desharp.Debug.DumpAndDie(object obj = null, DumpOptions? options = default(DumpOptions?));
```

## All Log Methods

Log exception instance as dumped string into exceptions.log|exceptions.html file. It stores:  
- exception type
- exception message
- if exception has been caught or not caught
- exception hash id
- error file where exception has been thrown
- thread call stack
All inner exceptions after this exception in the same way.
```cs
Desharp.Debug.Log(Exception exception = null);
```

Log any type value to application *.log|*.html file, specified by level param.
```cs
Desharp.Debug.Log(object obj = null, Level level = Level.INFO, int maxDepth = 0, int maxLength = 0);
```

## All Other Methods

Print to application output or log into file (if enabled) first param to be true  
or not and describe what was equal or not in first param by second param message.
```cs
Desharp.Debug.Assert(bool assertion, string description = "", Level logLevel = Level.DEBUG);
```

Configure Desharp assembly from running application environment and override  
any XML config settings or automatically detected settings.
```cs
Desharp.Debug.Configure(DebugConfig cfg);
```

Enable or disable variables dumping from application code environment for all threads  
or get enabled/disabled Desharp dumping state if no boolean param provided.
```cs
Desharp.Debug.Enabled(bool? enabled = default(bool?));
```

Return last uncaught Exception in request, mostly used in web applications by  
error page rendering process to know something about Exception before.
```cs
Desharp.Debug.GetLastError();
```

Return spent request processing time for web applications or return application  
up time for all other platforms.
```cs
Desharp.Debug.GetProcessingTime();
```

Print current thread stack trace into application output and exit running application.
In web applications - stop current request, in any other applications - stop application
with all it's threads and exit.
```cs
Desharp.Debug.Stop();
```

Prints to output or into log file number of seconds from last timer call under
called name in seconds with 3 floating point decimal spaces.
If no name specified or name is empty string, there is returned:
Web applications - number of seconds from request beginning.
Desktop applications - number of seconds from application start.
```cs
Desharp.Debug.Timer(string name = null, bool returnTimerSeconds = false, Level logLevel = Level.DEBUG);
```
