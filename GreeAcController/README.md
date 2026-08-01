# Gree AC Controller (Prototype)

A small .NET 10 console prototype that discovers, binds to, and controls a Gree
Wi-Fi air conditioner, based on the reverse-engineered protocol documented in
[tomikaa87/gree-remote](https://github.com/tomikaa87/gree-remote).

## What it does

- **Discovers** Gree units on the local network via a UDP broadcast "scan" on port 7000.
- **Binds** to the selected unit to obtain its device-specific AES key.
- **Turns the unit ON/OFF.**
- **Sets the target temperature** (Celsius).
- **Reads the current room temperature** from the unit's internal sensor (`TemSen`).
- Shows a quick status table (power, target and room temperature).

## Protocol notes

- All communication is UDP on port **7000**.
- Payloads are JSON, with a `pack` field that is AES-128/ECB/PKCS7-encrypted and Base64-encoded.
- Scanning and binding use a shared **generic key** (`a3K8Bx%2r8Y7#xDh`).
- After binding, the device returns a **unique key** used for all subsequent status/command requests.
- The room temperature sensor value (`TemSen`) has a `+40` offset to avoid negative numbers on the wire.

See `Gree/GreeCrypto.cs`, `Gree/GreeClient.cs`, and `Gree/AcController.cs` for the implementation.

## Running

```powershell
dotnet run
```

The app will scan the local network for a few seconds. If no device is found automatically
(e.g. due to network/firewall restrictions), you can enter the device's IP and MAC address manually.

> **Note:** Discovery and control require the console app to run on the same LAN/subnet as the
> AC unit's Wi-Fi module, and UDP broadcast/port 7000 traffic must not be blocked by firewalls.

## Disclaimer

This is a prototype for experimentation. It has not been tested against every Gree firmware
variant — some units may use slightly different parameter names or response formats as noted
in the upstream repository's README.
