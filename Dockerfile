FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CS2_MCP.csproj ./
RUN dotnet restore CS2_MCP.csproj

COPY . ./
RUN dotnet publish CS2_MCP.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 3001
ENV ASPNETCORE_URLS=http://0.0.0.0:3001
ENV CS2_WEB_UI_URL=http://0.0.0.0:3001

COPY --from=build /app/publish ./

USER $APP_UID

ENTRYPOINT ["dotnet", "CS2_MCP.dll"]
