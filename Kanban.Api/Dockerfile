FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Kanban.Api/Kanban.Api.csproj", "Kanban.Api/"]
RUN dotnet restore "Kanban.Api/Kanban.Api.csproj"

COPY . .
WORKDIR "/src/Kanban.Api"
RUN dotnet publish "Kanban.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Kanban.Api.dll"]
