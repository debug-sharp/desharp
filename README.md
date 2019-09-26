# Desharp - C# & VB .NET Debugging Tool

[![Latest Stable Version](https://img.shields.io/badge/Stable-v1.3.0.1-brightgreen.svg?style=plastic)](https://github.com/tomFlidr/desharp/releases)
[![License](https://img.shields.io/badge/Licence-BSD-brightgreen.svg?style=plastic)](https://raw.githubusercontent.com/debug-sharp/desharp/master/LICENCE.md)
![.NET Version](https://img.shields.io/badge/.NET->=4.0-brightgreen.svg?style=plastic)


---


## Outline
- [**About**](#about)
- [**Instalation**](#instalation)
- [**Demos & Examples**](#demos--examples)
- [**Do Not Miss**](#do-not-miss)
- [**Usage In Code**](#usage-in-code)
  - [**Basic Dumping & Logging Any Structuralized Variables**](#basic-dumping--logging-any-structuralized-variables)
  - [**Basic Dumping & Logging Exceptions**](#basic-dumping--logging-exceptions)
  - [**All Dump Methods**](#all-dump-methods)
  - [**All Log Methods**](#all-log-methods)
  - [**All Other Methods**](#all-other-methods)
- [**Dumps & Logs Outputs**](#dumps--logs-outputs)
- [**What You Can Dump Or Log**](#what-you-can-dump-or-log)
- [**What Is Rendered (Dumps & Logs)**](#what-is-rendered-dumps--logs)
- [**Configuration**](#configuration)
  - [**Configuration Examples**](#configuration-examples)
  - [**Runtime Configuration Options**](#runtime-configuration-options)
  - [**All XML Configuration Options**](#all-xml-configuration-options)
- [**Visual Studio - Code Snippets**](#visual-studio---code-snippets)


---


## About

**C# & VB .NET debugging utility for:**
- dump or log **any** structuralized variables
- dump or log exceptions with stack trace and inner exceptions

**Dump any variables into:**
- Console window
- Output debug panel in Visual Studio 
- Web response in floating window
- Html/text log files on HDD
- Any WinForms or WPF field component


---


## Instalation

[Desharp on Nuget.org](https://www.nuget.org/packages/Desharp/)

```nuget
PM> Install-Package Desharp
```


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


## Usage In Code

### Basic Dumping & Logging Any Structuralized Variables

#### C# Basic Example
```cs
using Desharp;
using System.Collections.Generic;

var list = new List<int?>() { 100, 200, null };
Debug.Dump(list);  // print list by Console.WriteLine(); or append into html response as floating window
Debug.Log(list);   // store dumped list in debug.log or debug.html file on HDD
```

#### VB Basic Example
```vb
Imports Desharp
Imports System.Collections.Generic

Dim list As New List(Of Int32?)() { 100, 200, null }
Debug.Dump(list)  ' print list by Console.WriteLine(); or append into html response as floating window
Debug.Log(list)   ' store dumped list in debug.log or debug.html file on HDD
```
Dumped result for both languages:
```
[List<Int32?>[3]]
   0: 100 [Int32]
   1: 200 [Int32]
   2: null
```

### Basic Dumping & Logging Exceptions

#### C# Basic Example
```cs
try {
   throw new Exception("Something wrong!");
} catch (Exception e) {
   Debug.Dump(e);  // print exception by Console.WriteLine(); or append into html response as floating window
   Debug.Log(e);   // store dumped exception in exception.log or exception.html file on HDD
}
```

#### VB Basic Example
```vb
Try
   Throw New Exception("Something wrong!")
Catch e As Exception
   Debug.Dump(e)  ' print exception by Console.WriteLine(); or append into html response as floating window
   Debug.Log(e)   ' store dumped exception in exception.log or exception.html file on HDD
End Try
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

### All Dump Methods

```cs
/**
 * Dump any values to application output (in web applications into debug bar,  
 * in desktop applications into console or debug output window)
 */
Desharp.Debug.Dump(params object[] args);

/**
 * Dump exception instance to application output if output dumping is enabled. It renders:
 * - exception type, exception message and exception hash id
 * - yes/no if exception has been caught or not caught
 * - error file where exception has been thrown
 * - thread call stack
 * All inner exceptions after this exception in the same way.
 */
Desharp.Debug.Dump(Exception exception = null, DumpOptions? options = default(DumpOptions?));

/**
 * Dump any values to application output (in web applications into debug bar,  
 * in desktop applications into console or debug output window)
 * This method dumps only single object with dump options like to dump
 * different object depth as usual, different string length or to dump source location and more...
 */
Desharp.Debug.Dump(object obj, DumpOptions? options = default(DumpOptions?));

/**
 * Dump any type value to direct application output (not into web request debug
 * bar in web applications!) and stop request/thread (in web applications dump into
 * direct response body, in desktop applications into console or debug output window).
 */
Desharp.Debug.DumpAndDie(object obj = null, DumpOptions? options = default(DumpOptions?));
```

### All Log Methods
```cs
/**
 * Log exception instance as dumped string into exceptions.log|exceptions.html file. It stores:
 * - exception type
 * - exception message
 * - if exception has been caught or not caught
 * - exception hash id
 * - error file where exception has been thrown
 * - thread call stack
 * All inner exceptions after this exception in the same way.
 */
Desharp.Debug.Log(Exception exception = null);

/**
 * Log any type value to application *.log|*.html file, specified by level param.
 */
Desharp.Debug.Log(object obj = null, Level level = Level.INFO, int maxDepth = 0, int maxLength = 0);
```

### All Other Methods
```cs
/**
 * Print to application output or log into file (if enabled) first param to be true
 * or not and describe what was equal or not in first param by second param message.
 */
Desharp.Debug.Assert(bool assertion, string description = "", Level logLevel = Level.DEBUG);

/**
 * Configure Desharp assembly from running application environment and override
 * any XML config settings or automatically detected settings.
 */
Desharp.Debug.Configure(DebugConfig cfg);

/**
 * Enable or disable variables dumping from application code environment for all threads 
 * or get enabled/disabled Desharp dumping state if no boolean param provided.
 */
Desharp.Debug.Enabled(bool? enabled = default(bool?));

/**
 * Return last uncaught Exception in request, mostly used in web applications by
 * error page rendering process to know something about Exception before.
 */
Desharp.Debug.GetLastError();

/**
 * Return spent request processing time for web applications or return application
 * up time for all other platforms.
 */
Desharp.Debug.GetProcessingTime();

/**
 * Print current thread stack trace into application output and exit running application.
 * In web applications - stop current request, in any other applications - stop application
 * with all it's threads and exit.
 */
Desharp.Debug.Stop();

/**
 * Prints to output or into log file number of seconds from last timer call under
 * called name in seconds with 3 floating point decimal spaces.
 * If no name specified or name is empty string, there is returned:
 * Web applications - number of seconds from request beginning.
 * Desktop applications - number of seconds from application start.
 */
Desharp.Debug.Timer(string name = null, bool returnTimerSeconds = false, Level logLevel = Level.DEBUG);
```

---


## Dumps & Logs Outputs

- console window for console applications
- Visual Studio console for Windows Forms or WPF applications
- floating window in html response for web applications
- special HTTP headers for FilePHP browser extension for web applications
- file logs in text/html formats


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

## What Is Rendered (Dumps & Logs)

- variables
  - always is rendered/logged the dumped variable:-)
  - dump is rendered with configurable return flag (to return dumped result as string)
  - dump or log is rendered with configurable:
    - source location call (default: `false`)
    - dump depth (default: `3`)
    - maximum string length (default: `1024`)

- exceptions
  - dump or log has always rendered
    - exception message, process and thread id
    - file and line, where exception has been dumped, logged or thrown
      (only if source code is possible to target by `*.PDB` file, there is rendered  
      a few lines from source code, where the  `Debug.dump()` or `Debug.Log()` 
	  has been called or where `Exception` has been thrown)
    - exception callstack
      (with editor opener links in web dump or in html log)
    - all inner exceptions inside the given exception
  - exceptions in web environment has always rendered:
    - requested URL, all http request headers and client IP
    - all loaded assemblies
    - there are different dump rendering for catched and not catched exceptions:
      - if optional flag for catched exception is `true`, exception is rendered as
	    openable exception in floating bar in left bottom screen corner
	  - if optional flag for catched exception is `false`, exception is rendered 
	    over whole browser window immediately with possibility to close it back into floating bar 


---


## Configuration

You can configure the `Desharp` utility by:
- `app.config` or `web.config` - more in [**all XML configuration options**](#all-xml-configuration-options)
- anytime directly by calling `Debug.Configure()` from any thread - more in [**runtime configuration options**](#runtime-configuration-options)

You can configure:
- if dumping/debugging is enabled or not (enabled by default after nuget package installation)
- debug IPs to enable debugging only for listed client IPs (localhost IPs by default)
- logs directory (`~/Logs` by default, relative from app root)
- logs file format:
  - html (`*.html`, by default in config file for all apps)
  - text (`*.log`, by default for desktop apps if config file is missing)
- logging levels:
  - debug (by default for all variables)
  - exception (by default for logged exceptions)
  - info
  - notice
  - warning
  - error
  - critical
  - alert
  - emergency
  - javascript
- favourite editor to open files from html debug output or log file by `editor://` protocol (`MSVS` by default)
- source file location to render where the `Desharp` library has been called in your source code


### Configuration Examples

After instaling **Desharp nuget package**, there are **automatically added** subnodes  
into your `App.config` or into `Web.config` file (if exists):
- into node `<appSettings>` - for desktop and web applications
- into node `<system.webServer>` - for web applications only

#### Console Apps, WinForms & WPF Apps:

```xml
<appSettings>
  ...
  <add key="Desharp:Enabled" value="1" />
  <add key="Desharp:Output" value="html" />
  <add key="Desharp:Levels" value="exception,debug,info,-notice,-warning,error,critical,alert,emergency,javascript" />
  <add key="Desharp:Directory" value="~/Logs" />
</appSettings>
```

#### Website Apps:

```xml
<appSettings>
  ...
  <add key="Desharp:Enabled" value="1" />
  <add key="Desharp:Output" value="html" />
  <add key="Desharp:DebugIps" value="127.0.0.1,::1" />`
  <add key="Desharp:Levels" value="exception,debug,info,-notice,-warning,error,critical,alert,emergency,javascript" />
  <add key="Desharp:Panels" value="Desharp.Panels.SystemInfo,Desharp.Panels.Session" />
  <add key="Desharp:Directory" value="~/Logs" />
</appSettings>
...
<system.webServer>
  ...
  <modules>
    ...
    <add name="Desharp" type="Desharp.Module" preCondition="managedHandler" />
  </modules>
  <httpErrors errorMode="Detailed" />
  ...
</system.webServer>
```

### Runtime Configuration Options

To overwrite XML config settings in running application or to temporary define different configuration, 
you can in any application place and in any thread to reconfigure `Desharp` utility (for all threads) to call:
```cs
Desharp.Debug.Configure(new Desharp.DebugConfig {
   Enabled = true,
   SourceLocation = true,
   Directory = "~/Logs",
   LogWriteMilisecond = 10000,
   Depth = 3,
   // `EnvType.Web` or `EnvType.Windows`, used very rarely:
   EnvType = EnvType.Web,
   // `Desharp.LogFormat.Html` or `Desharp.LogFormat.Text`:
   LogFormat = Desharp.LogFormat.Html,
   // for web apps only:
   ErrorPage = "~/custom-error-page.html",
   Panels = new[] { typeof(Desharp.Panels.SystemInfo), typeof(Desharp.Panels.Session) }
});
```

### All XML Configuration Options

After instaling **Desharp nuget package**, there are **automatically added**  
new xml file into your project root directory with name `Desharp.config.example`,  
where are all detailed configuration options you can copy and paste:


```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
  </startup>
  
  <!-- For web applications only - automatically added after nuget package installation: -->
  <system.webServer>
    <modules>
      <add name="Desharp" type="Desharp.Module" preCondition="managedHandler" />
    </modules>
    <httpErrors errorMode="Detailed" />
  </system.webServer>
  
  <appSettings>
    
    <!-- 
      If `true`, all dumps by method `Debug.Dump()` are internally rendered and displayed.
      For web applications, there is debug panel is displayed in browser with dumped variables or rendered exceptions.
      For desktop applications, there is console priniting enabled or output debug window printing enabled.
      - Not required to configure, but very recomanded.
      - Possible values: `1`, `0`, `true`, `false`.
      - If not configured - enabled is only when VS debugger is attached or entry assembly is builded as Debug.
      - If disabled - all `Debug.Log()` calls are still enabled and executed, see more option `Desharp:Levels`.
    -->
    <add key="Desharp:Enabled" value="1" />
    
    <!--
      Logs content format.
      - Not required, `text` by default.
      - Possible values: `html`, `text`.
    -->
    <add key="Desharp:Output" value="html" />
    
    <!--
      Client IP adresses list to limit `Desharp` enabled state only for some clients.
      - Not required to configure, very recomanded for web applications.
      - Possible values: IPv4 or IPv6, separated by comma (IPv6 without `[]` brackets).
      - If not configured and `Desharp` is in enabled state, then `Desharp` is enabled for all clients.
    -->
    <add key="Desharp:DebugIps" value="127.0.0.1,::1" />
    
    <!--
      Loggin levels to enable/disable to write on hard drive and also possibility to enable/disable email notifications.
      - Not required, recomanded.
      - Possible values: `exception`, `debug`, `info`, `notice`, `warning`, `error`, `critical`, `alert`, `emergency`, `javascript`.
      - If not configured, all logging levels are enabled for logging and not enabled for email notifications.
      - If at least one level is configured, then all other configured levels are disabled for logging and for email notifications.
      - If you want to enable any logging level - put the level name into node `value` attribute (comma separated).
      - If you want to disable any logging level - put minus (-) character before level name or remove level name.
      - If you want to enable any logging level for email notifications - put plus (+) character before level name.
		For any notification type with plus sign, it required to configure `Desharp:NotifySettings` property!
    -->
    <add key="Desharp:Levels" value="+exception,debug,info,-notice,-warning,+error,+critical,alert,+emergency,javascript" />

    <!--
      Logged messages notifications by configured levels.
      - Not required, recomanded in production mode.
      - Possible values: 
        - `host`: Required, mail server smtp domain | IPv4 | IPv6.
        - `port`: Not required, `25` by default.
        - `ssl`: Not required, `false` by default.
        - `from`: Required if no username and password specified, email address to specify sender, if no value specified, there is used `username` value.
        - `username` and `password`: Required if no `from` sender specified, mail server username/password credentials for sender account, always necessary to use together.
		- `domain` - Not required, no default value. Used only as third parametter for `System.Net.NetworkCredential` if presented.
        - `to`: Required, single recepient email adress or multiple adresses separated by semicolon `;`.
        - `priority`: Not required, possible values: `low` | `normal` | `high` (`normal` by defaut).
        - `timeout`: Not required, smtp server timeout specified in miliseconds, `10000` by default (10 seconds).
		- `background`: Not required at all. Default value is `true` to send all notifications in background thread. 
		  Use `false` value only to debug email sending.
    -->
    <add key="Desharp:NotifySettings" value="{
		host: 'smtp.company.com',
		port: 587,
		ssl: true,
		user: 'noreply@company.com',
		password: 'your-secret-password',
		from: 'noreply@company.com',
		to: 'your.name@gmail.com',
		priority: 'high',
		timeout: 30000
    }" />
    
    <!--
      Web debug bar panels.
      - Not required, recomanded.
      - Full class names separated by comma `,`.
      - Panel class has to implement public interface: `Desharp.Panels.IPanel`
      - Panel class could implement interface: `Desharp.Panels.ISessionPanel`,
        where are method called when session is is read and written every request.
      - There are always enabled build-in panels for execution time, dumps and exceptions.
      - Build-in panels you can optionally use: 
        - `Desharp.Panels.SystemInfo` - to display most important request info
        - `Desharp.Panels.Session` - to display basic session configuration and values
        - `Desharp.Panels.Routing` - to display matched MVC routes (still in TODO state)
    -->
    <add key="Desharp:Panels" value="Desharp.Panels.SystemInfo,Desharp.Panels.Session" />
    
    <!--
      Absolute or relative path from application root directory.
      - Not required, recomanded.
      - Relative path from app root has to start with '~/' like: '~/Path/To/Logs'.
      - If not configured, all log files are written into application root directory.
    -->
    <add key="Desharp:Directory" value="~/Logs" />
    
    <!-- 
      Always render source location from where dump has been called by `Debug.Dump()`.
      - Not required, recomanded.
      - Possible values: `1`, `0`, `true`, `false`.
      - If not configured, no dumps source locations are rendered in dump ouputs.
    -->
    <add key="Desharp:SourceLocation" value="1" />
    
    <!--
      Milisecond timeout how often logged messages or exceptions are written from memory to hard drive.
      - Not required, recomanded for speed optimalization in production mode.
      - Possible values - use digits to define any miliseconds integer value.
      - If not configured, all messages or exceptions are written immediately 
        in current thread, where is called `Desharp.Log()`.
    -->
    <add key="Desharp:WriteMiliseconds" value="5000" />
    
    <!--
      .NET objects dumping depth.
      - Not required, recomanded for speed optimalization in production mode.
      - Possible values: just digit like `2`, `3`, `4` or `5`.
      - If not configured, `3` by default.
    -->
    <add key="Desharp:Depth" value="3" />
    
    <!--
      Maximum length for dumped string values.
      - Not required, recomanded for speed optimalization in production mode.
      - Possible values: just digit like `512`, `1024` or `5000`...
      - If not configured, `1024` by default.
    -->
    <add key="Desharp:MaxLength" value="1024" />

    <!--
      Custom web error page.
      - Not required, recomanded for production mode.
      - If `Desharp` is not enabled and there is uncaught exception in your application,
        you can use custom static error page to transfer into client browser.
      - If not configured - `Desharp` build-in error page is used by default with error 500.
    -->
    <add key="Desharp:ErrorPage" value="~/custom-error-page-500.html" />

    <!--
      Dump all internal .NET events, properties ad fields in custom class instances, 
      (created internally and usually for properties values )and all class instance 
      member types marked with `System.Runtime.CompilerServices.CompillerGenerated` 
      attribute or with `Desharp.Hidden` attribute.
      This option is usefull to see everything in memory.
      - Not required, recomanded only for deep development view.
      - Possible values: `1`, `0`, `true`, `false`.
      - If not configured, `false` by default.
      - If not configured or configured as `false`, you can hide 
        your class members for dumping and logging by attributes:
        - [System.Runtime.CompilerServices.CompilerGenerated] - .NET standard.
        - [Desharp.Hidden] - shorter.
    -->
    <add key="Desharp:DumpCompillerGenerated" value="true" />
    
    <!-- 
      Default editor param value.
      - Not required, marginal.
      - For automatic file opening in Visual Studio or in any other editor by rendered links:
        `<a href="editor://file=...&line=...&editor=MSVS2019">../File.cs:123</a>
      - Possible values: any string key to open your editor from html output by.
      - If not configured, value is automaticly detected by Visual Studio instalation on current system.
    -->
    <add key="Desharp:Editor" value="MSVS2019" />
    
  </appSettings>
</configuration>
```

---


## Visual Studio - Code Snippets

To use long **[Desharp](https://www.nuget.org/packages/Desharp/)** calls more comfortable, install [Visual Studio code snippets](https://github.com/tomFlidr/desharp-codesnippets) for desharp to create proper shortcuts
More info [about code snippets for Visual Studio](https://code.visualstudio.com/docs/editor/userdefinedsnippets).

