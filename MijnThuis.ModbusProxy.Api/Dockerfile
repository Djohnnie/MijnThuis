FROM mcr.microsoft.com/dotnet/aspnet:9.0

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
ENV MODBUS_ADDRESS=
ENV MODBUS_PORT=

# Start the app
ENTRYPOINT ["dotnet", "MijnThuis.ModbusProxy.Api.dll"]