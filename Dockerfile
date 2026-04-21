# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore as distinct layers
COPY ["BNU-Student-Portal-Web/BNU-Student-Portal-Web.csproj", "BNU-Student-Portal-Web/"]
COPY ["BNU-Student-Portal-Presentation/BNU-Student-Portal-Presentation.csproj", "BNU-Student-Portal-Presentation/"]
COPY ["BNU-Student-Portal-Persistence/BNU-Student-Portal-Persistence.csproj", "BNU-Student-Portal-Persistence/"]
COPY ["BNU-Student-Portal-Services-Implementation/BNU-Student-Portal-Services-Abstraction.csproj", "BNU-Student-Portal-Services-Implementation/"]
COPY ["BNU-Student-Portal-Services/BNU-Student-Portal-Services.csproj", "BNU-Student-Portal-Services/"]
COPY ["BNU-Student-Portal-Domain/BNU-Student-Portal-Domain.csproj", "BNU-Student-Portal-Domain/"]
COPY ["BNU-Student-Portal-Shared-Library/BNU-Student-Portal-Shared-Library.csproj", "BNU-Student-Portal-Shared-Library/"]

RUN dotnet restore "BNU-Student-Portal-Web/BNU-Student-Portal-Web.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/BNU-Student-Portal-Web"
RUN dotnet build "BNU-Student-Portal-Web.csproj" -c Release -o /app/build

# ---- Publish Stage ----
FROM build AS publish
RUN dotnet publish "BNU-Student-Portal-Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BNU-Student-Portal-Web.dll"]
