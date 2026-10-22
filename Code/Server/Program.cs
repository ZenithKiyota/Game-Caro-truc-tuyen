using System.Net;
using CaroOnline.Server;
using CaroOnline.Shared;

// Cú pháp: CaroServer [--address|-a 0.0.0.0] [--port|-p 45454]
string address = "0.0.0.0";
int port = Protocol.DefaultPort;
for (int i = 0; i < args.Length; i++)
{
    string value = i + 1 < args.Length ? args[i + 1] : "";
    switch (args[i])
    {
        case "-a" or "--address": address = value; i++; break;
        case "-p" or "--port":
            if (!int.TryParse(value, out port) || port < 1 || port > 65535)
            {
                Console.Error.WriteLine("Port must be in 1..65535");
                return 2;
            }
            i++;
            break;
        case "-h" or "--help":
            Console.WriteLine("UDM_16 - TCP Game Caro Server\nCaroServer [--address 0.0.0.0] [--port 45454]");
            return 0;
        default:
            Console.Error.WriteLine($"Unknown option: {args[i]}");
            return 2;
    }
}
if (!IPAddress.TryParse(address, out IPAddress? bindAddress))
{
    Console.Error.WriteLine($"Invalid bind address: {address}");
    return 2;
}

using var server = new CaroServer();
if (!server.Start(bindAddress, port)) return 1;

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
await server.RunAsync(stop.Token);
return 0;
