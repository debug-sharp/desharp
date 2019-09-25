# Desharp - C#/VB .NET Debugging Tool

[![Latest Stable Version](https://img.shields.io/badge/Stable-v1.3.0-brightgreen.svg?style=plastic)](https://github.com/tomFlidr/desharp/releases)
[![License](https://img.shields.io/badge/Licence-BSD-brightgreen.svg?style=plastic)](https://raw.githubusercontent.com/debug-sharp/desharp/master/LICENCE.md)
![.NET Version](https://img.shields.io/badge/.NET->=4.0-brightgreen.svg?style=plastic)


C#/VB .NET debuging utility to dump or log structuralized variables, exceptions, stack traces and much more into console window, Visual Studio output panel, into html web response as floating window, into html/text log files on HDD or into any form field component.


## Outline
- [**Instalation**](#instalation)
- [**Demos & Examples**](#demos--examples)
- [**Do not miss**](#do-not-miss)
- [**Usage in code**](#usage-in-code)
- [**Configuration**](#configuration)
- [**Dumps & logs outputs**](#dumps--logs-outputs)
- [**Dumps & logs possibilities**](#dumps--logs-possibilities)
- [**VS Code snippets**](#vs-code-snippets)


---


### Instalation
[Desharp on Nuget.org](https://www.nuget.org/packages/Desharp/)

```nuget
PM> Install-Package Desharp
```


### Demos & Examples

- [**Console Application Demo (C#)**](https://github.com/debug-sharp/example-console-csharp), [**(VB.NET)**](https://github.com/debug-sharp/example-console-visualbasic)  
  Demo dumps and exceptions rendering into console window, logging on HDD and optional tests running.
- [**Windows Forms Application Demo (C#)**](https://github.com/debug-sharp/example-win-forms)
- [**Web Basic Application Demo (C#)**](https://github.com/debug-sharp/example-web-basic)
- [**Web MVC Application Demo (C#)**](https://github.com/debug-sharp/example-web-mvc)
- [**Web Forms Application Demo (C#)**](https://github.com/debug-sharp/example-web-forms)

### Do not miss
- Visual Studio code snippets for most offten `Desharp` calls
- Visual Studio file:line opener from rendered logs and exceptions

### Code snippets
To use long **[Desharp](https://www.nuget.org/packages/Desharp/)** calls more comfortable, install [Visual Studio code snippets](https://github.com/tomFlidr/desharp-codesnippets) for desharp to create proper shortcuts
More info [about code snippets for Visual Studio](https://code.visualstudio.com/docs/editor/userdefinedsnippets).


### Dump/log any variable
#### C#:
```cs
using Desharp;
using System.Collections.Generic;

var list = new List<string>() { "a", "b", "c" };
Debug.Dump(list);  // print list by Console.WriteLine(); or append into html response as floating window
Debug.Log(list);   // store dumped list in debug.log or debug.html file on HDD
```
#### VB:
```vb
Imports Desharp
Imports System.Collections.Generic

Dim list As New List(Of String)() { "a", "b", "c" }
Debug.Dump(list)  ' print list by Console.WriteLine(); or append into html response as floating window
Debug.Log(list)   ' store dumped list in debug.log or debug.html file on HDD
```
Dumped list for both languages in debug output or console window:
```
[List<string>(3)]
   0: "a" [String(1)]
   1: "b" [String(1)]
   2: "c" [String(1)]
```

### Dump/log `Exception`
#### C#:
```cs
try {
   throw new Exception("Something wrong!");
} catch (Exception e) {
   Debug.Dump(e);  // print exception by Console.WriteLine(); or append into html response as floating window
   Debug.Log(e);   // store dumped exception in exception.log or exception.html file on HDD
}
```
#### VB:
```vb
Try
   Throw New Exception("Something wrong!")
Catch e As Exception
   Debug.Dump(e)  ' print exception by Console.WriteLine(); or append into html response as floating window
   Debug.Log(e)   ' store dumped exception in exception.log or exception.html file on HDD
End Try
```
Dumped exception for both languages in debug output or console window:
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

## Dump outputs

- console window for console applications
- Visual Studio console for Windows Forms or WPF applications
- floating window in html response for web applications
- special HTTP headers for FilePHP browser extension for web applications
- file logs in text/html formats


## Dump possibilities

You can dump or log:
- any variables
  - primitive variables and it's primitive arrays like: `char`, `int?[]` and more 
  - `Lists`, `Arrays` or `Dictionaries` (`IList`, `IEnumerable`, `ICollection`, `IDictionary`...)
  - database results (`DataSet`, `DataTable`, `DataRow`)
  - **any custom class instances** with rendered events targets and properties and fields values
  - **anonymous objects**
  - much more... you can try:-)
- exceptions
  - all inner exceptions inside will be also dumped or logged


## Dump additional info

All dump or log calls have automaticly rendered:
- process/thread/request id
- current time from `DateTime.Now`
- callstack, where was `Debug.Dump()` or Debug.Log() used or where `Exception` happend
- if exception has been caused by another exception (inner exceptions), there is rendered source exception object hash
- if source code is possible by *.PDB file, there is rendered:
  - a few lines from source code, where was `Debug.Dump()` or `Debug.Log()` used or where `Exception` happend
- if environment is web application, there is rendered:
  - request URL
  - all http request headers
  - client IP
- if environment is web application, for dump browser output are rendered loaded assemblies


## Configuration

You can configure by `app.config`/`web.config` or directly by calling `Debug.Configure()`:
- if debugging is enabled or not (enabled by default)
- debug IPs to enable debugging only for list of client IPs (no ips by default)
- logs directory (app root if not defined)
- log files format:
  - text (*.log, by default)
  - html (*.html)
- logging levels:
  - debug (by default)
  - exception
  - info
  - notice
  - warning
  - error
  - critical
  - alert
  - emergency
  - javascript
- favourite editor to open files from html output by `editor://` protocol (MSVS by default)

### Configuration examples

#### Console, Windows forms or WPF application `App.config`:
- after instaling Desharp Nuget package, there are automaticly added appSettings keys bellow into your App.config:
  - `<add key="Desharp:Enabled" value="1" />`
  - `<add key="Desharp:Output" value="html" />`
  - `<add key="Desharp:Levels" value="+exception,debug,info,-notice,-warning,+error,+critical,alert,+emergency,javascript" />`
  - `<add key="Desharp:Directory" value="~/logs" />`
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <appSettings>
        
        <!-- 
            Web debug panel enabled od console priniting enabled
            - not required, recomanded
            - possible values: 1, 0, true, false
            - if not configured - enabled is only when VS debugger is attached or entry assembly is builded as Debug
            - if disabled - all Debug.Log() calls are still enabled, see more option "Desharp:Levels"
        -->
        <add key="Desharp:Enabled" value="1" />
        
        <!--
            Logs content format
            - not required, text by default
            - possible values: html, text
        -->
        <add key="Desharp:Output" value="html" />
        
        <!--
            Loggin levels to enable/disable to write on hard drive and also to enable/disable for email notification
            - not required, recomanded
            - possible values: exception, debug, info, notice, warning, error, critical, alert, emergency, javascript
            - if not configured, all logging levels are enabled for logging, not enabled for email notification
            - if at least one level is configured, then all other configured levels are disabled for logging and for email notification
            - if you want to enable any logging level - write level name bellow
            - if you want to disable any logging level - put minus (-) character before level name or remove level name
            - if you want to enable any logging level for email notification - put plus (+) character before level name
        -->
        <add key="Desharp:Levels" value="+exception,debug,info,-notice,-warning,+error,+critical,alert,+emergency,javascript" />
        
        <!--
            Logged messages notification by configured levels
            - not required, recomanded in production mode
            - possible values: 
                - host: required, mail server smtp domain | IPv4 | IPv6
                - port: not required, 25 by default
                - ssl: not required, false by default
                - from: required if no username and password specified, email address to specify sender, if no value specified, there is used username
                - username and password: required if no from sender specified, mail server username/password for sender account, always necessary to use together
                - to: required, recepient email adress or adresses separated by semicolon ';'
                - priority: not required, possible values: 'low' | 'normal' | 'high', 'normal' by defaut
                - timeout: not required, time cpecified in miliseconds, 10000 by default (10 seconds)
        -->
        <add key="Desharp:NotifySettings" value="{
            host: 'smtp.host.com',
            port: 25,
            ssl: false,
            user: 'username',
            password: 'secret',
            from: 'username@host.com',
            to: 'mydaily@mailbox.com;myother@mailbox.com',
            priority: 'high',
            timeout: 30000
        }" />
        
        <!--
            Absolute or relative path from application root directory
            - not required, recomanded
            - relative path from app root has to start with '~/' like: '~/path/to/logs'
            - if not configured, all log files are written into application root directory
        -->
        <add key="Desharp:Directory" value="~/logs" />
        
        <!--
            Milisecond timeout how often logged messages or exceptions are written from RAM to HDD
            - not required, recomanded for speed optimalization in production mode
            - possible values - use digits to define any miliseconds number
            - if not configured, all messages or exceptions are written in singleton background thread immediately
        -->
        <add key="Desharp:WriteMiliseconds" value="5000" />
        
        <!--
            C# object dumping depth
            - not required, recomanded for speed optimalization in production mode
            - possible values: just digit like 3, 4 or 5
            - if not configured, 3 by default
        -->
        <add key="Desharp:Depth" value="3" />
        
        <!--
            Max length for dumped string values
            - not required, recomanded for speed optimalization in production mode
            - possible values: just digit like 512, 1024 or 4000
            - if not configured, 1024 by default
        -->
        <add key="Desharp:MaxLength" value="512" />

        <!--
            Dump all backing fields in class instances, created usually for properties 
            and all class instance member types marked with 'CompillerGenerated' attribute.
            - not required, recomanded only for depp development view
            - possible values: 1, 0, true, false
            - if not configured, false by default
            - if not configured or configured as false, you can hide for dumping
        -->
        <add key="Desharp:DumpCompillerGenerated" value="true" />
        
        <!-- 
            Default editor param value
            - not required, marginal
            - for file opening links by 'editor://file=...&line=...&editor=MSVS'
            - possible values: any string key to open your editor from html output by
            - if not configured, value is automaticly detected by Visual Studio instalation on current system
        -->
        <add key="Desharp:Editor" value="MSVS" />
        
    </appSettings>
</configuration>
```

#### Website application `Web.config`:
- after instaling Desharp Nuget package, there are automaticly added new Desharp http module and appSettings keys bellow into your Web.config:
  - `<add key="Desharp:Enabled" value="1" />`
  - `<add key="Desharp:Output" value="html" />`
  - `<add key="Desharp:DebugIps" value="127.0.0.1,::1" />`
  - `<add key="Desharp:Levels" value="+exception,debug,info,-notice,-warning,+error,+critical,alert,+emergency,javascript" />`
  - `<add key="Desharp:Panels" value="Desharp.Panels.Session" />`
  - `<add key="Desharp:Directory" value="~/logs" />`
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    
    <startup>
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
    </startup>
    
    
    <!-- For web applications only - you have to add Desharp http module by: -->
    <system.webServer>
        <modules>
            <add name="Desharp" type="Desharp.Module" />
        </modules>
        <httpErrors errorMode="DetailedLocalOnly" />
    </system.webServer>
    
    
    <appSettings>
        
        <!-- 
            Web debug panel enabled od console priniting enabled
            - not required, recomanded
            - possible values: 1, 0, true, false
            - if not configured - enabled is only when VS debugger is attached or entry assembly is builded as Debug
            - if disabled - all Debug.Log() calls are still enabled, see more option "Desharp:Levels"
        -->
        <add key="Desharp:Enabled" value="1" />
        
        <!--
            Logs content format
            - not required, text by default
            - possible values: html, text
        -->
        <add key="Desharp:Output" value="html" />
        
        <!--
            Client ip adresses list to limit enabled desharp only for some users
            - not required, recomanded
            - possible values: IPv4 or IPv6, separated by comma
            - if not configured and desharp is enabled, then is enabled for all client ips
        -->
        <add key="Desharp:DebugIps" value="127.0.0.1,::1" />
        
        <!--
            Loggin levels to enable/disable to write on hard drive and also to enable/disable for email notification
            - not required, recomanded
            - possible values: exception, debug, info, notice, warning, error, critical, alert, emergency, javascript
            - if not configured, all logging levels are enabled for logging, not enabled for email notification
            - if at least one level is configured, then all other configured levels are disabled for logging and for email notification
            - if you want to enable any logging level - write level name bellow
            - if you want to disable any logging level - put minus (-) character before level name or remove level name
            - if you want to enable any logging level for email notification - put plus (+) character before level name
        -->
        <add key="Desharp:Levels" value="+exception,debug,info,-notice,-warning,+error,+critical,alert,+emergency,javascript" />

        <!--
            Logged messages notification by configured levels
            - not required, recomanded in production mode
            - possible values: exception, debug, info, notice, warning, error, critical, alert, emergency, javascript
            - if not configured, all logging levels are enabled for logging, not enabled for email notification
            - if at least one level is configured, then all other configured levels are disabled for logging and for email notification
            - if you want to enable any logging level - write level name bellow
            - if you want to disable any logging level - put minus (-) character before level name or remove level name
            - if you want to enable any logging level for email notification - put plus (+) character before level name
        -->
        <add key="Desharp:NotifySettings" value="{
            host: 'smtp.host.com',
            port: 25,
            ssl: false,
            user: 'username',
            password: 'secret',
            from: 'username@host.com',
            to: 'mydaily@mailbox.com',
            priority: 'high',
            timeout: 30000
        }" />
        
        <!--
            Web debug bar panels
            - not required, recomanded
            - full class names separated by comma character
            - all panel classes has to implement abstract class: Desharp.Panels.Abstract
            - build-in panels you can use: Desharp.Panels.Session (Desharp.Panels.Routing - TODO)
            - there are always enabled build-in panels for execution time, dumps and exceptions
        -->
        <add key="Desharp:Panels" value="Desharp.Panels.Session" />
        
        <!--
            Absolute or relative path from application root directory
            - not required, recomanded
            - relative path from app root has to start with '~/' like: '~/path/to/logs'
            - if not configured, all log files are written into application root directory
        -->
        <add key="Desharp:Directory" value="~/logs" />
        
        <!--
            Milisecond timeout how often logged messages or exceptions are written from RAM to HDD
            - not required, recomanded for speed optimalization in production mode
            - possible values - use digits to define any miliseconds number
            - if not configured, all messages or exceptions are written in singleton background thread immediately
        -->
        <add key="Desharp:WriteMiliseconds" value="5000" />
        
        <!--
            C# object dumping depth
            - not required, recomanded for speed optimalization in production mode
            - possible values: just digit like 3, 4 or 5
            - if not configured, 3 by default
        -->
        <add key="Desharp:Depth" value="3" />
        
        <!--
            Max length for dumped string values
            - not required, recomanded for speed optimalization in production mode
            - possible values: just digit like 512, 1024 or 4000
            - if not configured, 1024 by default
        -->
        <add key="Desharp:MaxLength" value="512" />

        <!--
            Custom web error page
            - not required, recomanded for production mode to be original
            - if desharp is not enabled and there is uncatched exception in your application,
              you can use custom static error page to transfer into client browser
            - if not configured - desharp build-in error page is used by default
        -->
        <add key="Desharp:ErrorPage" value="~/custom-error-page-500.html" />

        <!--
            Dump all backing fields in class instances, created usually for properties 
            and all class instance member types marked with 'CompillerGenerated' attribute.
            - not required, recomanded only for depp development view
            - possible values: 1, 0, true, false
            - if not configured, false by default
            - if not configured or configured as false, you can hide for dumping
        -->
        <add key="Desharp:DumpCompillerGenerated" value="true" />
        
        <!-- 
            Default editor param value
            - not required, marginal
            - for file opening links by 'editor://file=...&line=...&editor=MSVS'
            - possible values: any string key to open your editor from html output by
            - if not configured, value is automaticly detected by Visual Studio instalation on current system
        -->
        <add key="Desharp:Editor" value="MSVS" />
        
    </appSettings>
</configuration>
```

Any place inside your application to overwrite config settings:
```cs
Debug.Configure(new DebugConfig {
  Enabled = true,
  Directory = "~/logs",
  OutputType = OutputType.Html,
  LogWriteMilisecond = 10000,
  Depth = 3
});
```
