FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build

WORKDIR /src
COPY ABUS.DeviceConnectivity.SimulatedDevice/ABUS.DeviceConnectivity.SimulatedDevice.csproj ABUS.DeviceConnectivity.SimulatedDevice/
COPY ABUS.DeviceConnectivity.Messages/ABUS.DeviceConnectivity.Messages.csproj ABUS.DeviceConnectivity.Messages/

COPY NuGet.Config.Docker NuGet.Config
RUN  sed -i "s/SECRET/n5qbfytwsnq3ccfnhagei7ybeyb725rubkca4u2ccbtox3rz5lhq/g" NuGet.Config &&\
sed -i "s/USERNAME/christian.gottinger@live.com/g" NuGet.Config
RUN dotnet restore ABUS.DeviceConnectivity.SimulatedDevice/ABUS.DeviceConnectivity.SimulatedDevice.csproj
COPY . .
WORKDIR /src/ABUS.DeviceConnectivity.SimulatedDevice
RUN dotnet build ABUS.DeviceConnectivity.SimulatedDevice.csproj -c Release -o /app


FROM build AS publish
RUN dotnet publish ABUS.DeviceConnectivity.SimulatedDevice.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .


ENTRYPOINT ["dotnet", "ABUS.DeviceConnectivity.SimulatedDevice.dll"]