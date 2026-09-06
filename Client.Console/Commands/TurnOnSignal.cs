using TpLink.Api;

namespace TpLink.Cli.Commands;

public class TurnOnSignal : ICommand
{
    public async Task Execute(ITpLinkClient powerLine)
    {
        Console.WriteLine("turning on 5ghz and 2.4ghz signal!");
        await powerLine.ChangeWireless5GStatusAsync(true);
        await powerLine.ChangeWireless2GStatusAsync(true);
    }
}
