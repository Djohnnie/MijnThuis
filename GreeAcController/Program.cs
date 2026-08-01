using GreeAcController.Gree;
using Spectre.Console;

AnsiConsole.Write(new FigletText("Gree AC Controller").Color(Color.Cyan1));
AnsiConsole.MarkupLine("[grey]Prototype based on the reverse-engineered protocol from tomikaa87/gree-remote[/]");
AnsiConsole.WriteLine();

var client = new GreeClient();

var device = await DiscoverAndBindAsync(client);
if (device is null)
{
    AnsiConsole.MarkupLine("[red]No device selected. Exiting.[/]");
    return;
}

var controller = new AcController(client, device);
await RunMenuAsync(controller, device);

static async Task<GreeDevice?> DiscoverAndBindAsync(GreeClient client)
{
    var networks = GreeClient.GetLocalNetworks();
    if (networks.Count == 0)
    {
        AnsiConsole.MarkupLine("[red]Could not find any routable local network (with a default gateway). Are you connected to Wi-Fi/Ethernet?[/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[grey]Detected local network(s):[/]");
        foreach (var network in networks)
        {
            AnsiConsole.MarkupLine($"  [grey]- {network}[/]");
        }
    }

    List<GreeDevice> devices = [];

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync("Scanning local network for Gree devices (broadcast)...", async ctx =>
        {
            devices = await client.DiscoverAsync(TimeSpan.FromSeconds(3));
        });

    if (devices.Count == 0 && networks.Count > 0)
    {
        AnsiConsole.MarkupLine("[yellow]Broadcast scan found nothing (this is common with client/AP isolation, VLANs or firewalls).[/]");
        var sweep = AnsiConsole.Confirm("Try a slower subnet sweep instead (pings every address on your local /24)?", true);

        if (sweep)
        {
            var network = networks.Count == 1
                ? networks[0]
                : AnsiConsole.Prompt(
                    new SelectionPrompt<LocalNetworkInfo>()
                        .Title("Which network is the AC unit on?")
                        .UseConverter(n => n.ToString())
                        .AddChoices(networks));

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Sweeping {network.Address}/24, this can take a few seconds...", async ctx =>
                {
                    devices = await client.DiscoverBySweepAsync(network.Address.ToString());
                });
        }
    }

    if (devices.Count == 0)
    {
        AnsiConsole.MarkupLine("[red]No devices found on the network.[/]");
        var manual = AnsiConsole.Confirm("Do you want to enter a device IP address manually?", false);
        if (!manual)
        {
            AnsiConsole.MarkupLine("[grey]Tip: check your router's DHCP client list or use a network scanner app (e.g. Fing) to find the AC's IP.[/]");
            return null;
        }

        var ip = AnsiConsole.Ask<string>("Enter device IP address:");
        var mac = AnsiConsole.Ask<string>("Enter device MAC address (as reported by the app, no colons):");
        devices.Add(new GreeDevice
        {
            Mac = mac,
            Name = "Manual entry",
            IpAddress = System.Net.IPAddress.Parse(ip)
        });
    }

    var selected = devices.Count == 1
        ? devices[0]
        : AnsiConsole.Prompt(
            new SelectionPrompt<GreeDevice>()
                .Title("Select the AC unit to control:")
                .UseConverter(d => d.ToString())
                .AddChoices(devices));

    var bound = false;
    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync($"Binding to {selected.Name}...", async ctx =>
        {
            bound = await client.BindAsync(selected);
        });

    if (!bound)
    {
        AnsiConsole.MarkupLine("[red]Failed to bind to the device.[/]");
        return null;
    }

    AnsiConsole.MarkupLine($"[green]Bound successfully to {selected.Name} ({selected.Mac}) at [bold]{selected.IpAddress}[/] using {selected.EncryptionMode} encryption.[/]");
    return selected;
}

static async Task RunMenuAsync(AcController controller, GreeDevice device)
{
    while (true)
    {
        AnsiConsole.WriteLine();
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{device.Name}[/] [grey]({device.IpAddress})[/] - what do you want to do?")
                .AddChoices(
                    "Show status",
                    "Turn ON",
                    "Turn OFF",
                    "Set target temperature",
                    "Get room temperature",
                    "Exit"));

        try
        {
            switch (choice)
            {
                case "Show status":
                    await ShowStatusAsync(controller, device);
                    break;
                case "Turn ON":
                    await controller.TurnOnAsync();
                    AnsiConsole.MarkupLine("[green]Turned ON.[/]");
                    break;
                case "Turn OFF":
                    await controller.TurnOffAsync();
                    AnsiConsole.MarkupLine("[yellow]Turned OFF.[/]");
                    break;
                case "Set target temperature":
                    var temp = AnsiConsole.Prompt(
                        new TextPrompt<int>("Target temperature in \u00b0C (16-30):")
                            .Validate(t => t is >= 16 and <= 30 ? ValidationResult.Success() : ValidationResult.Error("Must be between 16 and 30")));
                    await controller.SetTargetTemperatureAsync(temp);
                    AnsiConsole.MarkupLine($"[green]Target temperature set to {temp}\u00b0C.[/]");
                    break;
                case "Get room temperature":
                    var roomTemp = await controller.GetRoomTemperatureAsync();
                    AnsiConsole.MarkupLine(roomTemp is null
                        ? "[red]Room temperature not available from this unit.[/]"
                        : $"[cyan]Current room temperature: {roomTemp}\u00b0C[/]");
                    break;
                case "Exit":
                    return;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
        }
    }
}

static async Task ShowStatusAsync(AcController controller, GreeDevice device)
{
    var isOn = await controller.IsPoweredOnAsync();
    var targetTemp = await controller.GetTargetTemperatureAsync();
    var roomTemp = await controller.GetRoomTemperatureAsync();

    var table = new Table().AddColumn("Property").AddColumn("Value");
    table.AddRow("IP address", device.IpAddress.ToString());
    table.AddRow("MAC address", device.Mac);
    table.AddRow("Power", isOn ? "[green]ON[/]" : "[grey]OFF[/]");
    table.AddRow("Target temperature", $"{targetTemp}\u00b0C");
    table.AddRow("Room temperature", roomTemp is null ? "n/a" : $"{roomTemp}\u00b0C");

    AnsiConsole.Write(table);
}
