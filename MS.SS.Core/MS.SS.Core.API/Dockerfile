FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY MS.SS.Core.API/MS.SS.Core.API.csproj MS.SS.Core.API/
COPY MS.SS.Core.App/MS.SS.Core.App.csproj MS.SS.Core.App/
COPY MS.SS.Core.Infrastructure/MS.SS.Core.Infrastructure.csproj MS.SS.Core.Infrastructure/
COPY MS.SS.Core.Modules.Identity/MS.SS.Core.Modules.Identity.csproj MS.SS.Core.Modules.Identity/
COPY MS.SS.Core.Modules.Reception/MS.SS.Core.Modules.Reception.csproj MS.SS.Core.Modules.Reception/
COPY MS.SS.Core.Security/MS.SS.Core.Security.csproj MS.SS.Core.Security/
COPY MS.SS.Core.SharedKernel/MS.SS.Core.SharedKernel.csproj MS.SS.Core.SharedKernel/
RUN dotnet restore MS.SS.Core.API/MS.SS.Core.API.csproj
COPY . .
RUN dotnet publish MS.SS.Core.API/MS.SS.Core.API.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "MS.SS.Core.API.dll"]
