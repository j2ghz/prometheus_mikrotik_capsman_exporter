FROM microsoft/dotnet:sdk
WORKDIR /app
ADD MikrotikExporter/ /app
RUN dotnet restore && dotnet build
EXPOSE 1234
ENTRYPOINT [ "dotnet", "run" ]
HEALTHCHECK CMD curl --fail http://localhost:1234/metrics || exit 1
