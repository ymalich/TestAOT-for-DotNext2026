rem dotnet publish TestAot.csproj
dotnet publish TestAot2.csproj -o ./publish/r2r-fd -p:PublishReadyToRun=true --self-contained false

pause