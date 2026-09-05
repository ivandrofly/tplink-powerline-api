using TpLink.Api;
using TpLink.Cli;

// usage: dotnet run --project Client.Console -- <on|off|reboot|clients|batch>
var actions = new Dictionary<string, (string Help, Func<Invoker, Task> Run)>(StringComparer.OrdinalIgnoreCase)
{
    ["on"] = ("turn the 2.4 GHz and 5 GHz radios on", invoker => invoker.TurnOn()),
    ["off"] = ("turn the 2.4 GHz and 5 GHz radios off", invoker => invoker.TurnOff()),
    ["reboot"] = ("reboot the adapter", invoker => invoker.Reboot()),
    ["clients"] = ("list the wireless clients connected to the adapter", invoker => invoker.DisplayClient()),
    ["batch"] = ("list clients, reboot, wait a minute, list clients again", invoker => invoker.RunBatch()),
};

if (args.Length != 1 || !actions.TryGetValue(args[0], out var action))
{
    Console.WriteLine("usage: dotnet run --project Client.Console -- <command>");
    Console.WriteLine();
    foreach (var (name, (help, _)) in actions)
    {
        Console.WriteLine($"  {name,-8} {help}");
    }

    Console.WriteLine();
    Console.WriteLine($"Credentials come from the {TpLinkOptions.LoginVariable} and {TpLinkOptions.PasswordVariable} environment variables.");
    Console.WriteLine($"Set {TpLinkOptions.EndpointVariable} (e.g. http://192.168.1.86) to skip discovery. Disable any VPN first.");
    return 1;
}

try
{
    using var invoker = new Invoker();
    await invoker.DiscoverAsync();
    await action.Run(invoker);
    return 0;
}
catch (Exception ex) when (ex is InvalidOperationException or TimeoutException or TpLinkException)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}
