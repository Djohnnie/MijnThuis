FROM mcr.microsoft.com/dotnet/runtime:9.0

# Set the working directory
WORKDIR /app

# Copy the app
COPY bin/Release/publish /app

EXPOSE 8080

ENV TZ=Europe/Brussels
ENV CONNECTIONSTRING=
ENV CLIENT_ID=
ENV CERTIFICATE_FILENAME=
ENV CERTIFICATE_PASSWORD=

# Start the app
ENTRYPOINT ["dotnet", "MijnThuis.Worker.dll"]