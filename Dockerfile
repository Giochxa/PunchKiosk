# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PunchKiosk/ ./
WORKDIR /src
RUN dotnet publish PunchKiosk.csproj -c Release -o /app/out

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "PunchKiosk.dll"]
