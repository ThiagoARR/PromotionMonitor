FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PromotionMonitor.sln ./
COPY src/PromotionMonitor.Domain/PromotionMonitor.Domain.csproj src/PromotionMonitor.Domain/
COPY src/PromotionMonitor.Application/PromotionMonitor.Application.csproj src/PromotionMonitor.Application/
COPY src/PromotionMonitor.Infrastructure/PromotionMonitor.Infrastructure.csproj src/PromotionMonitor.Infrastructure/
COPY src/PromotionMonitor.Api/PromotionMonitor.Api.csproj src/PromotionMonitor.Api/
RUN dotnet restore src/PromotionMonitor.Api/PromotionMonitor.Api.csproj
COPY src/ ./src/
RUN dotnet publish src/PromotionMonitor.Api/PromotionMonitor.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PromotionMonitor.Api.dll"]
