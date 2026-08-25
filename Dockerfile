FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY . .

RUN dotnet restore Random_Github.slnx

RUN dotnet publish RandomGithub.Web/RandomGithub.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

USER root

# Used by the Docker health check.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN chown -R "${APP_UID}:${APP_UID}" /app

USER ${APP_UID}

EXPOSE 5183

ENTRYPOINT ["dotnet", "RandomGithub.Web.dll"]