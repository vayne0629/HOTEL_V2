# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 先 copy csproj 讓 restore 可快取
COPY *.csproj ./
RUN dotnet restore

# 再 copy 其餘檔案
COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ===== run =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HotelAPI_V2.dll"]
