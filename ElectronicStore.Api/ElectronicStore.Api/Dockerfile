# Sử dụng image SDK để xây dựng (build) ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file project và khôi phục các dependency
# Tên project là ElectronicStore.Api
COPY ["ElectronicStore.Api.csproj", "."] 
RUN dotnet restore "ElectronicStore.Api.csproj"

# Sao chép toàn bộ source code
COPY . .

# Xuất bản (publish) ứng dụng ở chế độ Release
RUN dotnet publish "ElectronicStore.Api.csproj" -c Release -o /app/publish

# Sử dụng image Runtime nhỏ hơn để chạy (run) ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080 
COPY --from=build /app/publish .

# Định nghĩa điểm vào (entrypoint)
ENTRYPOINT ["dotnet", "ElectronicStore.Api.dll"]