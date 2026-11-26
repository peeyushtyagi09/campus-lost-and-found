FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/LostAndFound.Api/LostAndFound.Api.csproj src/LostAndFound.Api/
RUN dotnet restore src/LostAndFound.Api/LostAndFound.Api.csproj
COPY src/LostAndFound.Api/ src/LostAndFound.Api/
RUN dotnet publish src/LostAndFound.Api/LostAndFound.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/uploads && chown -R "$APP_UID:$APP_UID" /app
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet LostAndFound.Api.dll"]
