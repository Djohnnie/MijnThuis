FROM mcr.microsoft.com/dotnet/aspnet:9.0

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

# Start the app
ENTRYPOINT ["dotnet", "MijnThuis.Worker.dll"]