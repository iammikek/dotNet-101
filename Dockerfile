FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY dotNet-101.sln ./
COPY src/dotNet101.Api/dotNet101.Api.csproj src/dotNet101.Api/
COPY src/dotNet101.Application/dotNet101.Application.csproj src/dotNet101.Application/
COPY src/dotNet101.Domain/dotNet101.Domain.csproj src/dotNet101.Domain/
COPY src/dotNet101.Infrastructure/dotNet101.Infrastructure.csproj src/dotNet101.Infrastructure/
COPY tests/dotNet101.Api.Tests/dotNet101.Api.Tests.csproj tests/dotNet101.Api.Tests/

RUN dotnet restore

COPY src ./src
COPY tests ./tests

RUN dotnet publish src/dotNet101.Api/dotNet101.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8010

ENV ASPNETCORE_URLS=http://0.0.0.0:8010 \
    ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "dotNet101.Api.dll"]
