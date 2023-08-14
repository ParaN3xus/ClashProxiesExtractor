# ClashProxiesExtractor
Extract and aggregate proxies from several subscription url.

## Feature

- Extract proxy server info from subscription url.
- Aggregate proxies of several subscription.
- Fallback to the local file when failed to retrieve the latest proxy config.

## Usage

```css
http://localhost:56688/api/extract?urls=url1;url2;url3&names=name1;name2;name3
```

## Auto start(Windows)

Save code below to a vbs file and move it to `C:\Users\YOURUSERNAME\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup`, don't forget to replace `"path\to\ClashProxiesExtractor.exe"` with your path.

```vbscript
Set objShell = CreateObject("WScript.Shell")
appPath = "path\to\ClashProxiesExtractor.exe"

appDirectory = Left(appPath, InStrRev(appPath, "\"))
command = """" & appPath & """ /s"

objShell.CurrentDirectory = appDirectory
objShell.Run command, 0, True

Set objShell = Nothing
```

