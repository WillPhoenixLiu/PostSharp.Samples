FROM mcr.microsoft.com/dotnet/core/aspnet:2.1-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["Diagnostics/PostSharp.Samples.Logging.ElasticStack/ClientMvc/ClientMvc.csproj", "Diagnostics/PostSharp.Samples.Logging.ElasticStack/ClientMvc/"]
RUN dotnet restore "Diagnostics/PostSharp.Samples.Logging.ElasticStack/ClientMvc/ClientMvc.csproj"
COPY . .
WORKDIR "/src/Diagnostics/PostSharp.Samples.Logging.ElasticStack/ClientMvc"
RUN dotnet build "ClientMvc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClientMvc.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClientMvc.dll"]