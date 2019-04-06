FROM microsoft/dotnet:2.2-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk AS build

WORKDIR /src
COPY ConsoleApp3.csproj .
COPY Program.cs .

RUN dotnet restore ConsoleApp3.csproj
COPY . .
RUN dotnet build ConsoleApp3.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish ConsoleApp3.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .


ENTRYPOINT ["dotnet", "ConsoleApp3.dll"]