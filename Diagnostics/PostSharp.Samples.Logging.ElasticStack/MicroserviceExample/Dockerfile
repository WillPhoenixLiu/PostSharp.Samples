FROM mcr.microsoft.com/dotnet/core/aspnet:2.1-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["Diagnostics/PostSharp.Samples.Logging.ElasticStack/MicroserviceExample/MicroserviceExample.csproj", "Diagnostics/PostSharp.Samples.Logging.ElasticStack/MicroserviceExample/"]
RUN dotnet restore "Diagnostics/PostSharp.Samples.Logging.ElasticStack/MicroserviceExample/MicroserviceExample.csproj"
COPY . .
WORKDIR "/src/Diagnostics/PostSharp.Samples.Logging.ElasticStack/MicroserviceExample"
RUN dotnet build "MicroserviceExample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MicroserviceExample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MicroserviceExample.dll"]