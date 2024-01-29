FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 as app
EXPOSE 8080
RUN apt-get update && apt-get install -y ffmpeg
WORKDIR /app
RUN mkdir /app/tmp
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "tikthumb.dll"]
