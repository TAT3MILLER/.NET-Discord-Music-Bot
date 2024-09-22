FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN apt-get update
RUN apt-get install openjdk-17-jre -y
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DiscordMusicBot/DiscordMusicBot.csproj", "DiscordMusicBot/"]
RUN dotnet restore "DiscordMusicBot/DiscordMusicBot.csproj"
COPY . .
WORKDIR "/src/DiscordMusicBot"
RUN curl -L https://github.com/lavalink-devs/Lavalink/releases/download/4.0.7/Lavalink.jar > Lavalink.jar
RUN dotnet build "DiscordMusicBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DiscordMusicBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
ENTRYPOINT ["dotnet", "run"]

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiscordMusicBot.dll"]
