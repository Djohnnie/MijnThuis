FROM mcr.microsoft.com/dotnet/runtime:10.0

# Set the working directory
WORKDIR /app

# Copy the app
COPY bin/Release/publish /app

EXPOSE 8080

ENV TZ=Europe/Brussels
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV CONNECTIONSTRING=
ENV CLIENT_ID=
ENV CERTIFICATE_FILENAME=
ENV CERTIFICATE_PASSWORD=
ENV MODBUS_PROXY_CLIENT_SECRET=

# Start the app
ENTRYPOINT ["dotnet", "MijnThuis.Worker.dll"]