# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore as distinct layers
COPY ["YourAPP-Web/YourAPP-Web.csproj", "YourAPP-Web/"]
COPY ["YourAPP-Presentation/YourAPP-Presentation.csproj", "YourAPP-Presentation/"]
COPY ["YourAPP-Persistence/YourAPP-Persistence.csproj", "YourAPP-Persistence/"]
COPY ["YourAPP-Services-Implementation/YourAPP-Services-Abstraction.csproj", "YourAPP-Services-Implementation/"]
COPY ["YourAPP-Services/YourAPP-Services.csproj", "YourAPP-Services/"]
COPY ["YourAPP-Domain/YourAPP-Domain.csproj", "YourAPP-Domain/"]
COPY ["YourAPP-Shared-Library/YourAPP-Shared-Library.csproj", "YourAPP-Shared-Library/"]

RUN dotnet restore "YourAPP-Web/YourAPP-Web.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/YourAPP-Web"
RUN dotnet build "YourAPP-Web.csproj" -c Release -o /app/build

# ---- Publish Stage ----
FROM build AS publish
RUN dotnet publish "YourAPP-Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YourAPP-Web.dll"]
