FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY global.json ./
COPY HygiaTrade.Server.API/HygiaTrade.Server.API.csproj HygiaTrade.Server.API/
COPY HygiaTrade.Server.Data/HygiaTrade.Server.Data.csproj HygiaTrade.Server.Data/
COPY HygiaTrade.Server.Domain/HygiaTrade.Server.Domain.csproj HygiaTrade.Server.Domain/
COPY HygiaTrade.Server.Core/HygiaTrade.Server.Core.csproj HygiaTrade.Server.Core/
COPY HygiaTrade.Server.Common/HygiaTrade.Server.Common.csproj HygiaTrade.Server.Common/

RUN dotnet restore HygiaTrade.Server.API/HygiaTrade.Server.API.csproj

COPY . .
RUN dotnet publish HygiaTrade.Server.API/HygiaTrade.Server.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "HygiaTrade.Server.API.dll"]
