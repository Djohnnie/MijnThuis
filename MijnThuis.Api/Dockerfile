FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Set the working directory
WORKDIR /app

# Copy the app
COPY bin/Release/publish /app

EXPOSE 5000

ENV TZ=Europe/Brussels
ENV CONNECTIONSTRING=
ENV CLIENT_ID=
ENV CERTIFICATE_FILENAME=
ENV CERTIFICATE_PASSWORD=

# Start the app
ENTRYPOINT ["dotnet", "MijnThuis.Api.dll"]