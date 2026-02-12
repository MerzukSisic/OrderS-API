FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files
COPY ["OrdersAPI.API/OrdersAPI.API.csproj", "OrdersAPI.API/"]
COPY ["OrdersAPI.Application/OrdersAPI.Application.csproj", "OrdersAPI.Application/"]
COPY ["OrdersAPI.Domain/OrdersAPI.Domain.csproj", "OrdersAPI.Domain/"]
COPY ["OrdersAPI.Infrastructure/OrdersAPI.Infrastructure.csproj", "OrdersAPI.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "OrdersAPI.API/OrdersAPI.API.csproj"

# Copy everything else
COPY OrdersAPI.API/ OrdersAPI.API/
COPY OrdersAPI.Application/ OrdersAPI.Application/
COPY OrdersAPI.Domain/ OrdersAPI.Domain/
COPY OrdersAPI.Infrastructure/ OrdersAPI.Infrastructure/

# Build
WORKDIR "/src/OrdersAPI.API"
RUN dotnet build "OrdersAPI.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrdersAPI.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrdersAPI.API.dll"]