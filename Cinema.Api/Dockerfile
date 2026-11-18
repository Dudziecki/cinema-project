# Stage 1: restore and publish the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Cinema.Contracts/Cinema.Contracts.csproj", "Cinema.Contracts/"]
COPY ["Cinema.Api/Cinema.Api.csproj", "Cinema.Api/"]
RUN dotnet restore "Cinema.Api/Cinema.Api.csproj"

COPY . .
WORKDIR /src/Cinema.Api
RUN dotnet publish "Cinema.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: run only on the smaller ASP.NET runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cinema.Api.dll"]
