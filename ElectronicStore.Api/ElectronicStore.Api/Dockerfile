# Sử dụng image SDK để xây dựng (build) ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ElectronicStore.Api.csproj", "."] 
RUN dotnet restore "ElectronicStore.Api.csproj"

COPY . .
COPY Data ./Data
RUN dotnet publish "ElectronicStore.Api.csproj" -c Release -o /app/publish

# ---------------------------
# Image Runtime để chạy app
# ---------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# >>> FIX MÚI GIỜ VIỆT NAM <<<
ENV TZ=Asia/Ho_Chi_Minh
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
# >>> END FIX <<<

EXPOSE 8080
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ElectronicStore.Api.dll"]
