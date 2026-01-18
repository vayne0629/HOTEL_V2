# ===== build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 先複製 csproj 才能讓 restore 有快取
COPY HotelAPI_V2/*.csproj ./HotelAPI_V2/
RUN dotnet restore ./HotelAPI_V2/HotelAPI_V2.csproj

# 再複製整包程式碼
COPY . .

# 發佈
RUN dotnet publish ./HotelAPI_V2/HotelAPI_V2.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Render 常用 10000 port，但它會提供 PORT 環境變數；保險作法是用 ASPNETCORE_URLS 綁定到 10000
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HotelAPI_V2.dll"]
