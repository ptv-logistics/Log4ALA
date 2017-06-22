rmdir C:\Users\mob\.nuget\packages\Log4ALA /S /Q

REM and remove Log4ALA version from package folder of the project e.g. Dave which references the Log4ALA module
rmdir D:\produkte\dave\ExecutionMonitor_2016\packages\Log4ALA*  /S /Q
rmdir D:\produkte\dave\Log4ALA\packages\Log4ALA*  /S /Q

rem remove from local nuget server to test the changed module and deploy to local nuget server 
rmdir D:\produkte\dave\tools\NuPackageServer\Packages\Log4ALA  /S /Q

REM restart project e.g. dave or metrics and rebuild to get the new Log4ALA nuget package

"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" BuildProject.proj /p:Configuration="net45;net451;net452;net46;net461" /p:Targets="clean;build"
cd Log4ALA
..\packages\NuGet.CommandLine.4.1.0\tools\nuget.exe pack -MSBuildPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin"

ping 127.0.0.1 -n 15 > nul
xcopy *.nupkg D:\produkte\dave\tools\NuPackageServer\Packages
rem /S /E /Y


pause