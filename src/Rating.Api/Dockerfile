FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Rating.Api/Rating.Api.csproj src/Rating.Api/
RUN dotnet restore src/Rating.Api/Rating.Api.csproj
COPY src/Rating.Api/ src/Rating.Api/
RUN dotnet publish src/Rating.Api/Rating.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Rating.Api.dll"]