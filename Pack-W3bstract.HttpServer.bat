nuget pack ./W3bstract.HttpServer.nuspec -Build -Symbols -OutputDirectory ".\(Stage)\Packages" -InstallPackageToOutputPath
PAUSEIF NOT EXIST "..\(NuGetRepo)" GOTO NOCOPYTOGLOBALREPO
xcopy ".\(Stage)\Packages\*.nuspec" "..\(NuGetRepo)\" /d /r /y /s
xcopy ".\(Stage)\Packages\*.nupkg*" "..\(NuGetRepo)\" /d /r /y /s
:NOCOPYTOGLOBALREPO
PAUSE