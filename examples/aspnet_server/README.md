# ASP.NET Core Server Example

A minimal ASP.NET Core server that hosts a Robot Raconteur service over WebSockets. ASP.NET accepts WebSocket
connections on `/robotraconteur` and hands them to `TcpTransport.AcceptAndProcessServerWebSocket()`, so the
service shares the web server's port, TLS and hosting setup.

The service is the `Greeter` object defined in `experimental.aspnet_example.robdef`.

## Setup

Install the .NET 8 SDK from https://dotnet.microsoft.com/download, then install the `RobotRaconteur.Sharp.Gen`
dotnet tool. It generates the Robot Raconteur thunk code during the build.

```
dotnet tool install -g RobotRaconteur.Sharp.Gen
```

## Run Example

```
dotnet run
```

The server listens on `http://localhost:5000`. Connect a Robot Raconteur client using:

```
rr+ws://localhost:5000/robotraconteur?service=greeter
```

For example, from C#:

```csharp
var c = (experimental.aspnet_example.Greeter)await RobotRaconteurNode.s.ConnectService(
    "rr+ws://localhost:5000/robotraconteur?service=greeter");
Console.WriteLine(await c.say_hello("World"));
```

Behind an HTTPS server, use `rr+wss://`.

## Browser Clients

Browsers send an `Origin` header with WebSocket requests. ASP.NET accepts every origin unless you limit them, so
before serving browser clients, allow only the pages you trust using `WebSocketOptions.AllowedOrigins`. The
commented-out lines in `Program.cs` show how.
