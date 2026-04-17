FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["Lab5.Api.csproj", "./"]
RUN dotnet restore "Lab5.Api.csproj"
COPY . .
RUN dotnet build "Lab5.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Lab5.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lab5.Api.dll"]