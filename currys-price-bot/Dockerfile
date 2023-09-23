FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["currys-price-bot.csproj", "./"]
RUN dotnet restore "currys-price-bot.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "currys-price-bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "currys-price-bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "currys-price-bot.dll"]
