FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY *.sln .
COPY Directory.Packages.props .
COPY global.json .
COPY HygiaTrade.API/*.csproj ./HygiaTrade.API/
COPY HygiaTrade.Data/*.csproj ./HygiaTrade.Data/
COPY HygiaTrade.Domain/*.csproj ./HygiaTrade.Domain/
COPY HygiaTrade.Core/*.csproj ./HygiaTrade.Core/
COPY HygiaTrade.Common/*.csproj ./HygiaTrade.Common/
COPY HygiaTrade.Tests.Unit/*.csproj ./HygiaTrade.Tests.Unit/


RUN dotnet restore HygiaTrade.sln

COPY . .
RUN dotnet publish HygiaTrade.API/HygiaTrade.API.csproj -c Release -o out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 10000

ENTRYPOINT ["dotnet", "HygiaTrade.API.dll"]
