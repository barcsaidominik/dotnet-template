FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Template.Api/Template.Api.csproj", "src/Template.Api/"]
COPY ["src/Template.Application/Template.Application.csproj", "src/Template.Application/"]
COPY ["src/Template.Domain/Template.Domain.csproj", "src/Template.Domain/"]
COPY ["src/Template.Grpc/Template.Grpc.csproj", "src/Template.Grpc/"]
COPY ["src/Template.Infrastructure/Template.Infrastructure.csproj", "src/Template.Infrastructure/"]
COPY NuGet.Config ./
RUN dotnet restore "src/Template.Api/Template.Api.csproj"
COPY . .
WORKDIR "/src/src/Template.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore -p:OpenApiGenerateDocumentsOnBuild=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Template.Api.dll"]
