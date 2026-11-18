# Stage 1: build the Blazor Server application.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Cinema.Contracts/Cinema.Contracts.csproj", "Cinema.Contracts/"]
COPY ["Cinema.Blazor/Cinema.Blazor.csproj", "Cinema.Blazor/"]
RUN dotnet restore "Cinema.Blazor/Cinema.Blazor.csproj"

COPY . .
WORKDIR /src/Cinema.Blazor
RUN dotnet publish "Cinema.Blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cinema.Blazor.dll"]
