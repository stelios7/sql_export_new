@echo off
REM Publish your WPF app as a single .exe file

dotnet publish SQL_Export.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  --output .\publish

pause
