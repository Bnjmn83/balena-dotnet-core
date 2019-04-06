FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
ARG SECRET
ARG USERNAME
WORKDIR /src
COPY ABUS.DeviceConnectivity.SimulatedDevice/ABUS.DeviceConnectivity.SimulatedDevice.csproj ABUS.DeviceConnectivity.SimulatedDevice/
COPY ABUS.DeviceConnectivity.Messages/ABUS.DeviceConnectivity.Messages.csproj ABUS.DeviceConnectivity.Messages/

COPY NuGet.Config.Docker NuGet.Config
RUN  sed -i "s/SECRET/${SECRET}/g" NuGet.Config &&\
sed -i "s/USERNAME/${USERNAME}/g" NuGet.Config
RUN dotnet restore ABUS.DeviceConnectivity.SimulatedDevice/ABUS.DeviceConnectivity.SimulatedDevice.csproj
COPY . .
WORKDIR /src/ABUS.DeviceConnectivity.SimulatedDevice
RUN dotnet build ABUS.DeviceConnectivity.SimulatedDevice.csproj -c Release -o /app

FROM microsoft/dotnet:2.1-sdk AS test
ARG BUILD_NUMBER
ARG SECRET
ARG USERNAME
WORKDIR /src
COPY ABUS.DeviceConnectivity.Tests/ABUS.DeviceConnectivity.Tests.csproj ABUS.DeviceConnectivity.Tests/
COPY NuGet.Config.Docker NuGet.Config
RUN sed -i "s/USERNAME/${USERNAME}/g" NuGet.Config &&\
sed -i "s/SECRET/${SECRET}/g" NuGet.Config
COPY . .
WORKDIR /src/ABUS.DeviceConnectivity.Tests
RUN dotnet test ABUS.DeviceConnectivity.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput='/var/coverageresults/'
RUN dotnet reportgenerator "-reports:/var/coverageresults/coverage.cobertura.xml" "-targetdir:/var/coverageresults/reports/" -tag:$BUILD_NUMBER -reportTypes:htmlInline

FROM build AS publish
RUN dotnet publish ABUS.DeviceConnectivity.SimulatedDevice.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=test /var/coverageresults ./coverageresults
COPY --from=publish /app .
# startup script copies code coverage files and then launches .net core application
# The subfolder is required since docker-compose looks for the file in it�s launch folder
COPY ./ABUS.DeviceConnectivity.SimulatedDevice/startup.sh /usr/sbin/startup.sh
RUN chmod 777 /usr/sbin/startup.sh

ENTRYPOINT ["dotnet", "ABUS.DeviceConnectivity.SimulatedDevice.dll"]