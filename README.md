# Desharp - C# .NET Debugging Tool

## Examples

### Dump/log `Exception`
```cs
try {
  throw new Exception("Something wrong!");
} catch (Exception e) {
  Debug.Dump(e);  // print exception by Console.WriteLine(); or append into html response
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

