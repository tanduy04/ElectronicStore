# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ElectronicStore.WebApi/ElectronicStore.WebApi.csproj", "ElectronicStore.WebApi/"]
COPY ["ElectronicStore.WebApi.Application/ElectronicStore.WebApi.Application.csproj", "ElectronicStore.WebApi.Application/"]
COPY ["ElectronicStore.WebApi.Domain/ElectronicStore.WebApi.Domain.csproj", "ElectronicStore.WebApi.Domain/"]
COPY ["ElectronicStore.WebApi.Infrastructure/ElectronicStore.WebApi.Infrastructure.csproj", "ElectronicStore.WebApi.Infrastructure/"]
RUN dotnet restore "./ElectronicStore.WebApi/ElectronicStore.WebApi.csproj"
COPY . .
WORKDIR "/src/ElectronicStore.WebApi"
RUN dotnet build "./ElectronicStore.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ElectronicStore.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ElectronicStore.WebApi.dll"]