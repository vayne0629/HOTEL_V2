# ===============================
# BUILD
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 先複製 csproj（快取 restore）
COPY HotelAPI_V2/*.csproj ./HotelAPI_V2/
RUN dotnet restore ./HotelAPI_V2/HotelAPI_V2.csproj

# 再複製全部
COPY . .

# publish
RUN dotnet publish ./HotelAPI_V2/HotelAPI_V2.csproj -c Release -o /app/publish /p:UseAppHost=false


# ===============================
# RUNTIME
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HotelAPI_V2.dll"]
