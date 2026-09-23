rem dotnet publish TestAot.csproj
dotnet publish TestAot2.csproj -o ./publish/r2r-sc -p:PublishReadyToRun=true -p:PublishSingleFile=true  --self-contained true

pause