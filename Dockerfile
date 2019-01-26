FROM microsoft/dotnet:sdk
WORKDIR /app
ADD MikrotikExporter/ /app
RUN dotnet restore && dotnet build
EXPOSE 1234
ENTRYPOINT [ "dotnet", "run" ]
