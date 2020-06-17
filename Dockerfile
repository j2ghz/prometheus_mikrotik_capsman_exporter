FROM mcr.microsoft.com/dotnet/core/sdk:2.2
WORKDIR /app
ADD MikrotikExporter/ /app
RUN dotnet restore && dotnet build
EXPOSE 1234
ENTRYPOINT [ "dotnet", "run" ]
HEALTHCHECK CMD curl --fail http://localhost:1234/metrics || exit 1
