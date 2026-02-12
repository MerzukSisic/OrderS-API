FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["OrdersAPI.Worker/OrdersAPI.Worker.csproj", "OrdersAPI.Worker/"]
COPY ["OrdersAPI.Domain/OrdersAPI.Domain.csproj", "OrdersAPI.Domain/"]

# Restore
RUN dotnet restore "OrdersAPI.Worker/OrdersAPI.Worker.csproj"

# Copy source code
COPY OrdersAPI.Worker/ OrdersAPI.Worker/
COPY OrdersAPI.Domain/ OrdersAPI.Domain/

# Build
WORKDIR "/src/OrdersAPI.Worker"
RUN dotnet build "OrdersAPI.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrdersAPI.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrdersAPI.Worker.dll"]