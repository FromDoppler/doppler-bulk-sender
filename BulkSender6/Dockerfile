#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BulkSender6/Doppler.BulkSender.csproj", "BulkSender6/"]
RUN dotnet restore "BulkSender6/Doppler.BulkSender.csproj"
COPY . .
WORKDIR "/src/BulkSender6"
RUN dotnet build "Doppler.BulkSender.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Doppler.BulkSender.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Doppler.BulkSender.dll"]