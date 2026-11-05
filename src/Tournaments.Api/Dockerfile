FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Tournaments.Domain/Tournaments.Domain.csproj src/Tournaments.Domain/
COPY src/Tournaments.Api/Tournaments.Api.csproj src/Tournaments.Api/
RUN dotnet restore src/Tournaments.Api/Tournaments.Api.csproj
COPY src/Tournaments.Domain/ src/Tournaments.Domain/
COPY src/Tournaments.Api/ src/Tournaments.Api/
RUN dotnet publish src/Tournaments.Api/Tournaments.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tournaments.Api.dll"]