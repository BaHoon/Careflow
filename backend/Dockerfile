# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CareFlow.slnx", "./"]
COPY ["CareFlow.WebApi/CareFlow.WebApi.csproj", "CareFlow.WebApi/"]
COPY ["CareFlow.Application/CareFlow.Application.csproj", "CareFlow.Application/"]
COPY ["CareFlow.Core/CareFlow.Core.csproj", "CareFlow.Core/"]
COPY ["CareFlow.Infrastructure/CareFlow.Infrastructure.csproj", "CareFlow.Infrastructure/"]
COPY ["CareFlow.Tests/CareFlow.Tests.csproj", "CareFlow.Tests/"]
RUN dotnet restore "CareFlow.WebApi/CareFlow.WebApi.csproj"
COPY . .
WORKDIR "/src/CareFlow.WebApi"
RUN dotnet build "CareFlow.WebApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CareFlow.WebApi.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CareFlow.WebApi.dll"]
