# Desharp - C# .NET Debugging Tool



## Examples

### Dump/log any variable
```cs
var list = new List<string>() { "a", "b", "c" });
Debug.Dump(list);  // print list by Console.WriteLine(); or append into html response as floating window
Debug.Log(list);   // store list in hard drive in exception.log
```

### Dump/log `Exception`
```cs
try {
  throw new Exception("Something wrong!");
} catch (Exception e) {
  Debug.Dump(e);  // print exception by Console.WriteLine(); or append into html response as floating window
  Debug.Log(e);   // store exception in hard drive in exception.log
}
```

## Dump outputs

- console window for console application
- Visual Studio console for Windows Forms or WPF application
- floating window in html response for web applications
- headers formated for FilePHP browser extension for web applications
- file logs


## Dump possibilities

You can dump or log:
- any variables
  - primitive variables and it's primitive arrays
  - `Lists`, `Arrays` and `Dictionaries` (`IList`, `IEnumerable` and `IDictionary`)
  - database results (`DataSet`, `DataTable`, `DataRow`)
  - **custom classes** with properties and fields
  - **anonymous objects**
  - much more... you can try:-)
- exceptions


## Dump additional info

All dump or log calls have automaticly rendered:
- thread/request id
- current time from `DateTime.Now`
- call stack, where was `Debug.Dump()` or Debug.Log() used
- if source code is possible by *.PDB file, there is rendered:
  - a few lines from source code, where was `Debug.Dump()` or `Debug.Log()` used 
- if environment is web application, there is rendered:
  - request URL
  - all http request headers
  - client IP


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
- favourite editor to open files from html output by `editor://` protocol (MSVS2016 by default)

### Configuration examples

Console, Windows forms or WPF application `app.config`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <appSettings>
        <!-- values: 1, 0, true, false -->
	<add key="Desharp:Enabled" value="1" />
        <!--
	     values: any string key to open your editor from html output by: 
	     `editor://file=...&line=...&editor=MSVS2015
	-->
	<add key="Desharp:Editor" value="MSVS2015" />
        <!-- values: html, text -->
	<add key="Desharp:Output" value="text" />
        <!-- 
             values: list of keys bellow, 
             it's not necessary to use all, 
             all enabled by default, 
             if you want to disable any logging level - put minus char before level key
        -->
	<add key="Desharp:Levels" value="exception,debug,-info,-notice,-warning,error,critical,alert,emergency,javascript" />
        <!-- values: absolute path or relative path from application root starting with '~/' -->
	<add key="Desharp:Directory" value="~/logs" />
    </appSettings>
</configuration>
```

Website application `web.config`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <appSettings>
        <!-- values: 1, 0, true, false -->
	<add key="Desharp:Enabled" value="1" />
        <!--
	     values: any string key to open your editor from html output by: 
	     `editor://file=...&line=...&editor=MSVS2015
	-->
	<add key="Desharp:Editor" value="MSVS2015" />
        <!-- values: html, text -->
	<add key="Desharp:Output" value="html" />
        <!-- values: IPv4 or IPv6 separated by comma, only for web applications -->
	<add key="Desharp:DebugIps" value="127.0.0.1,88.31.45.67" />
        <!-- 
             values: list of keys bellow, 
             it's not necessary to use all, 
             all enabled by default, 
             if you want to disable any logging level - put minus char before level key
        -->
	<add key="Desharp:Levels" value="exception,debug,-info,-notice,-warning,error,critical,alert,emergency,javascript" />
        <!-- values: absolute path or relative path from application root starting with '~/' -->
	<add key="Desharp:Directory" value="~/logs" />
  </appSettings>
</configuration>
```

Any place inside your application to overwrite config settings:
```cs
Debug.Configure(new DebugConfig {
  Enabled = true,
  Directory = "~/logs",
  OutputType = OutputType.Html
});
```
