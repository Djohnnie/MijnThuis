FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Set the working directory
WORKDIR /app

# Copy the app
COPY bin/Release/publish /app

EXPOSE 8080

RUN apt-get update
RUN apt-get install -y locales locales-all
ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8
ENV TZ=Europe/Brussels
ENV CONNECTIONSTRING=
ENV CLIENT_ID=
ENV CERTIFICATE_FILENAME=
ENV CERTIFICATE_PASSWORD=

# Start the app
ENTRYPOINT ["dotnet", "MijnThuis.Dashboard.Web.dll"]