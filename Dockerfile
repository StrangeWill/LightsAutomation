FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

WORKDIR /app
EXPOSE 80


ENV ASPNETCORE_URLS=http://+:80

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
#RUN adduser -u 5678 --disabled-password --gecos "" appuser \
#    && mkdir -p /mnt/drop \
#    && chown -R appuser /app \
#    && chown -R appuser /mnt/drop
#USER appuser

#FROM node:18-bullseye-slim AS vuebuild
#WORKDIR /src
#COPY ["Lights.Vue", "Lights.Vue"]
#WORKDIR "/src/Lights.Vue"
#RUN npm ci
#RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
WORKDIR /src
COPY . .
WORKDIR "/src/Lights.Web"
RUN dotnet build "Lights.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#COPY --from=vuebuild /src/Lights.Web/wwwroot wwwroot
RUN rm /app/appsettings.local.json
ENTRYPOINT ["dotnet", "Lights.Web.dll"]
