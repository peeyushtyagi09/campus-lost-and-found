FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY LostAndFound.sln ./
COPY src/LostAndFound.Api/LostAndFound.Api.csproj src/LostAndFound.Api/
COPY tests/LostAndFound.Api.Tests/LostAndFound.Api.Tests.csproj tests/LostAndFound.Api.Tests/
RUN dotnet restore LostAndFound.sln
COPY . .
RUN dotnet publish src/LostAndFound.Api/LostAndFound.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/uploads && chown -R "$APP_UID:$APP_UID" /app
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LostAndFound.Api.dll"]
