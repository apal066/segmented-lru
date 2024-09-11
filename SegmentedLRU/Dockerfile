#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SegmentedLRU.csproj", "SegmentedLRU/"]
RUN dotnet restore "SegmentedLRU/SegmentedLRU.csproj"
COPY . "SegmentedLRU"
WORKDIR "/src/SegmentedLRU"
RUN dotnet build "SegmentedLRU.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SegmentedLRU.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SegmentedLRU.dll"]