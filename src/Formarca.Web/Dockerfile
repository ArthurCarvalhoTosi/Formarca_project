FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Formarca.Domain/Formarca.Domain.csproj src/Formarca.Domain/
COPY src/Formarca.Application/Formarca.Application.csproj src/Formarca.Application/
COPY src/Formarca.Infrastructure/Formarca.Infrastructure.csproj src/Formarca.Infrastructure/
COPY src/Formarca.Web/Formarca.Web.csproj src/Formarca.Web/
RUN dotnet restore src/Formarca.Web/Formarca.Web.csproj
COPY src/Formarca.Domain/ src/Formarca.Domain/
COPY src/Formarca.Application/ src/Formarca.Application/
COPY src/Formarca.Infrastructure/ src/Formarca.Infrastructure/
COPY src/Formarca.Web/ src/Formarca.Web/
RUN dotnet publish src/Formarca.Web/Formarca.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl fontconfig fonts-liberation \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Formarca.Web.dll"]
