SET MSBUILDPATH="C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"
SET MSBUILD=%MSBUILDPATH%\MSBuild.exe


REM %MSBUILD% build.proj /target:NuGetRestore
REM %MSBUILD% /p:Configuration=Release ..\Log4ALA.sln
REM %MSBUILD% build.proj /target:NuGetPack /property:Configuration=Release;RELEASE=true
REM PackageVersion=4.5.0;PatchVersion=0;PatchCoreVersion=0
%MSBUILD% build.proj /target:BuildAll /property:Configuration=Release;RELEASE=true;MajorVersion=2;MinorVersion=9;PatchVersion=1;MSBuildPath=%MSBUILDPATH%
pause