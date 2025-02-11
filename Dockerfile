FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 7070
EXPOSE 7071

# 証明書をコピーしてパーミッションを設定
COPY ["certs/aspnetapp.pfx", "/https/"]
USER root
RUN chown $APP_UID /https/aspnetapp.pfx && \
    chmod 600 /https/aspnetapp.pfx
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LLMWebAPI.csproj", "./"]
RUN dotnet restore "LLMWebAPI.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "LLMWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LLMWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LLMWebAPI.dll"]
