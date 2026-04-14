dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:SelfContained=false
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:SelfContained=false