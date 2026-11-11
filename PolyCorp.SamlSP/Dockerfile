
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PolyCorp.SamlSP.csproj", "./"]
RUN dotnet restore "PolyCorp.SamlSP.csproj"

COPY . .
RUN dotnet publish "PolyCorp.SamlSP.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=https://+:5001
EXPOSE 5001

ENTRYPOINT ["dotnet", "PolyCorp.SamlSP.dll"]
