#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["DockerWaitFor/DockerWaitFor.csproj", "DockerWaitFor/"]
RUN dotnet restore "DockerWaitFor/DockerWaitFor.csproj"
COPY . .
WORKDIR "/src/DockerWaitFor"
RUN dotnet build "DockerWaitFor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DockerWaitFor.csproj" -c Release -o /app/publish

#Todo: add user

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DockerWaitFor.dll"]