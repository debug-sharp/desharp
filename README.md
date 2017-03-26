# Desharp - C#/VB .NET Debugging Tool

C#/VB .NET debuging utility to dump or log structuralized variables, exceptions, stack traces and much more into console, visual studio console, into html web response as floating window or into html/text log files on HDD.

## Instalation
```nuget
PM> Install-Package Desharp
```

## Examples

### Dump/log any variable
```cs
var list = new List<string>() { "a", "b", "c" });
Debug.Dump(list);  // print list by Console.WriteLine(); or append into html response as floating window
Debug.Log(list);   // store dumped list in debug.log or debug.html file on HDD
```

### Dump/log `Exception`
```cs
try {
  throw new Exception("Something wrong!");
} catch (Exception e) {
  Debug.Dump(e);  // print exception by Console.WriteLine(); or append into html response as floating window
  Debug.Log(e);   // store dumped exception in exception.log or exception.html file on HDD
}
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
  - primitive variables and it's primitive arrays like: `char`, `int[]` and more 
  - `Lists`, `Arrays` or `Dictionaries` (`IList`, `IEnumerable`, `ICollection`, `IDictionary`...)
  - database results (`DataSet`, `DataTable`, `DataRow`)
  - **any custom class instances** with rendered property and field values
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
- favourite editor to open files from html output by `editor://` protocol (MSVS2015 by default)

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
            - not required, but practical to useing it
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
            - not required, but practical to useing it
            - possible values: exception, debug, info, notice, warning, error, critical, alert, emergency, javascript
            - if not configured, all logging levels are enabled for logging, not enabled for email notification
            - if at least one level is configured, then all other configured levels are disabled for logging and for email notification
            - if you want to enable any logging level - write level name bellow
            - if you want to disable any logging level - put minus (-) character before level name or remove level name
            - if you want to enable any logging level for email notification - put plus (+) character before level name
        -->
        <add key="Desharp:Levels" value="+exception,debug,info,-notice,-warning,+error,+critical,alert,+emergency,javascript" />
        
        <!--
            Absolute or relative path from application root directory
            - not required, but practical to useing it
            - relative path from app root has to start with '~/' like: '~/path/to/logs'
            - if not configured, all log files are written into application root directory
        -->
        <add key="Desharp:Directory" value="~/logs" />
        
        <!--
            Milisecond timeout how often logged messages or exceptions are written from RAM to HDD
            - not required, but very good for speed optimalization in production mode
            - possible values - use digits to define any miliseconds number
            - if not configured, all messages or exceptions are written in singleton background thread immediately
        -->
        <add key="Desharp:WriteMiliseconds" value="5000" />
        
        <!--
            C# object dumping depth
            - not required, but very good for speed optimalization in production mode
            - possible values: just digit like 3, 4 or 5
            - if not configured, 3 by default
        -->
        <add key="Desharp:Depth" value="3" />
        
        <!--
            Max length for dumped string values
            - not required, but very good for speed optimalization in production mode
            - possible values: just digit like 512, 1024 or 4000
            - if not configured, 1024 by default
        -->
        <add key="Desharp:MaxLength" value="512" />
        
        <!-- 
            Default editor param value
            - not required, marginal
            - for file opening links by 'editor://file=...&line=...&editor=MSVS2015'
            - possible values: any string key to open your editor from html output by
            - if not configured, value is automaticly detected by Visual Studio instalation on current system
        -->
        <add key="Desharp:Editor" value="MSVS2015" />
        
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
            - not required, but practical to useing it
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
            - not required, but practical to useing it
            - possible values: IPv4 or IPv6, separated by comma
            - if not configured and desharp is enabled, then is enabled for all client ips
        -->
        <add key="Desharp:DebugIps" value="127.0.0.1,::1" />
        
        <!--
            Loggin levels to enable/disable to write on hard drive and also to enable/disable for email notification
            - not required, but practical to useing it
            - possible values: exception, debug, info, notice, warning, error, critical, alert, emergency, javascript
            - if not configured, all logging levels are enabled for logging, not enabled for email notification
            - if at least one level is configured, then all other configured levels are disabled for logging and for email notification
            - if you want to enable any logging level - write level name bellow
            - if you want to disable any logging level - put minus (-) character before level name or remove level name
            - if you want to enable any logging level for email notification - put plus (+) character before level name
        -->
        <add key="Desharp:Levels" value="+exception,debug,info,-notice,-warning,+error,+critical,alert,+emergency,javascript" />
        
        <!--
            Web debug bar panels
            - not required, good for advanced C# developers who want to extend desharp tool
            - full class names separated by comma character
            - all panel classes has to implement abstract class: Desharp.Panels.Abstract
            - build-in panels you can use: Desharp.Panels.Session (Desharp.Panels.Routing - TODO)
            - there are always enabled build-in panels for execution time, dumps and exceptions
        -->
        <add key="Desharp:Panels" value="Desharp.Panels.Session" />
        
        <!--
            Absolute or relative path from application root directory
            - not required, but practical to useing it
            - relative path from app root has to start with '~/' like: '~/path/to/logs'
            - if not configured, all log files are written into application root directory
        -->
        <add key="Desharp:Directory" value="~/logs" />
        
        <!--
            Milisecond timeout how often logged messages or exceptions are written from RAM to HDD
            - not required, but very good for speed optimalization in production mode
            - possible values - use digits to define any miliseconds number
            - if not configured, all messages or exceptions are written in singleton background thread immediately
        -->
        <add key="Desharp:WriteMiliseconds" value="5000" />
        
        <!--
            C# object dumping depth
            - not required, but very good for speed optimalization in production mode
            - possible values: just digit like 3, 4 or 5
            - if not configured, 3 by default
        -->
        <add key="Desharp:Depth" value="3" />
        
        <!--
            Max length for dumped string values
            - not required, but very good for speed optimalization in production mode
            - possible values: just digit like 512, 1024 or 4000
            - if not configured, 1024 by default
        -->
        <add key="Desharp:MaxLength" value="512" />

        <!--
            Custom web error page
            - not required, but very good for production mode to be original
            - if desharp is not enabled and there is uncatched exception in your application,
              you can use custom static error page to transfer into client browser
            - if not configured - desharp build-in error page is used by default
        -->
        <add key="Desharp:ErrorPage" value="~/custom-error-page-500.html" />
        
        <!-- 
            Default editor param value
            - not required, marginal
            - for file opening links by 'editor://file=...&line=...&editor=MSVS2015'
            - possible values: any string key to open your editor from html output by
            - if not configured, value is automaticly detected by Visual Studio instalation on current system
        -->
        <add key="Desharp:Editor" value="MSVS2015" />
        
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

## Additional info
- Desharp library works in .NET framework >= 4.0
- Desharp is possible to use in Visual Basic application, but it was tested wery poorly
