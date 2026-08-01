FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY GrokCareerCoach.slnx .
COPY src/GrokCareerCoach.Api/GrokCareerCoach.Api.csproj src/GrokCareerCoach.Api/
RUN dotnet restore src/GrokCareerCoach.Api/GrokCareerCoach.Api.csproj

COPY src/GrokCareerCoach.Api/ src/GrokCareerCoach.Api/
RUN dotnet publish src/GrokCareerCoach.Api/GrokCareerCoach.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install --yes --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GrokCareerCoach.Api.dll"]
