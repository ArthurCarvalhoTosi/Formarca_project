FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Formarca.Domain/Formarca.Domain.csproj src/Formarca.Domain/
COPY src/Formarca.Application/Formarca.Application.csproj src/Formarca.Application/
COPY src/Formarca.Infrastructure/Formarca.Infrastructure.csproj src/Formarca.Infrastructure/
COPY src/Formarca.BackgroundRunner/Formarca.BackgroundRunner.csproj src/Formarca.BackgroundRunner/
RUN dotnet restore src/Formarca.BackgroundRunner/Formarca.BackgroundRunner.csproj
COPY src/Formarca.Domain/ src/Formarca.Domain/
COPY src/Formarca.Application/ src/Formarca.Application/
COPY src/Formarca.Infrastructure/ src/Formarca.Infrastructure/
COPY src/Formarca.BackgroundRunner/ src/Formarca.BackgroundRunner/
RUN dotnet publish src/Formarca.BackgroundRunner/Formarca.BackgroundRunner.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Formarca.BackgroundRunner.dll"]
