FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /source
COPY global.json ./
COPY backend/ ./backend/
RUN dotnet restore backend/src/Restaurant.API/Restaurant.API.csproj --locked-mode
RUN dotnet publish backend/src/Restaurant.API/Restaurant.API.csproj -c Release -o /app --no-restore /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0.13
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Restaurant.API.dll"]
