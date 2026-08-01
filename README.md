# MijnThuis
Home IoT solution for power consumption, solar energy, electric car charging, temperature sensors, electric switches, smart lights, sauna and airconditioning.

## Airconditioning (Gree)

The airconditioning integration controls a Gree Wi-Fi unit directly over the local network (UDP port 7000) using the reverse-engineered protocol from https://github.com/tomikaa87/gree-remote. Since discovery isn't used in production, configure the unit's IP and MAC address via environment variables:

- `AIRCONDITIONING_IP_ADDRESS` - the AC unit's static/reserved IP address (e.g. `192.168.10.50`)
- `AIRCONDITIONING_MAC_ADDRESS` - the AC unit's MAC address (e.g. `d8f1c8112233`, no separators)
