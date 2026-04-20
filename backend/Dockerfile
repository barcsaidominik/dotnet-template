FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/services/Template.Api/Template.Api.csproj", "src/services/Template.Api/"]
COPY ["src/core/Template.Application/Template.Application.csproj", "src/core/Template.Application/"]
COPY ["src/core/Template.Domain/Template.Domain.csproj", "src/core/Template.Domain/"]
COPY ["src/core/Template.Grpc/Template.Grpc.csproj", "src/core/Template.Grpc/"]
COPY ["src/core/Template.Infrastructure/Template.Infrastructure.csproj", "src/core/Template.Infrastructure/"]
COPY ["src/common/Template.Common/Template.Common.csproj", "src/common/Template.Common/"]
COPY ["src/common/Template.Common.Email/Template.Common.Email.csproj", "src/common/Template.Common.Email/"]
COPY ["src/common/Template.Common.Excel/Template.Common.Excel.csproj", "src/common/Template.Common.Excel/"]
COPY ["src/common/Template.Common.Jobs/Template.Common.Jobs.csproj", "src/common/Template.Common.Jobs/"]
COPY ["src/common/Template.Common.Pdf/Template.Common.Pdf.csproj", "src/common/Template.Common.Pdf/"]
COPY ["src/common/Template.Common.Templating/Template.Common.Templating.csproj", "src/common/Template.Common.Templating/"]
COPY NuGet.Config ./
RUN dotnet restore "src/services/Template.Api/Template.Api.csproj"
COPY . .
WORKDIR "/src/src/services/Template.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore -p:OpenApiGenerateDocumentsOnBuild=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Template.Api.dll"]
