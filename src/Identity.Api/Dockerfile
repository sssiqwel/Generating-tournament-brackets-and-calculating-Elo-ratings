FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Identity.Api/Identity.Api.csproj src/Identity.Api/
RUN dotnet restore src/Identity.Api/Identity.Api.csproj
COPY src/Identity.Api/ src/Identity.Api/
RUN dotnet publish src/Identity.Api/Identity.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]