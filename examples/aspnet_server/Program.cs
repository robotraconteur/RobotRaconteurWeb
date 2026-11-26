// Minimal ASP.NET Core server hosting a Robot Raconteur service over WebSockets.
// Clients connect with: rr+ws://localhost:5000/robotraconteur?service=greeter

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using RobotRaconteurSharp;

// Set up the Robot Raconteur node. The TcpTransport does not listen on its own port here;
// it only processes the WebSockets that ASP.NET accepts.
var tcp = new TcpTransport();
RobotRaconteurNode.s.RegisterTransport(tcp);
RobotRaconteurNode.s.RegisterServiceType(new experimental.aspnet_example.experimental__aspnet_exampleFactory());
RobotRaconteurNode.s.RegisterService("greeter", "experimental.aspnet_example", new GreeterImpl());

var app = WebApplication.CreateBuilder(args).Build();

// Browsers send an Origin header. To accept browser clients, allow only the pages you trust:
// var ws_options = new WebSocketOptions();
// ws_options.AllowedOrigins.Add("https://myapp.example.com");
// app.UseWebSockets(ws_options);
app.UseWebSockets();

app.Map("/robotraconteur", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    using var websocket = await context.WebSockets.AcceptWebSocketAsync("robotraconteur.robotraconteur.com");
    // Completes when the client disconnects
    await tcp.AcceptAndProcessServerWebSocket(websocket, context.Request.Path);
});

app.MapGet("/", () => "Robot Raconteur service: rr+ws://localhost:5000/robotraconteur?service=greeter");

app.Lifetime.ApplicationStopping.Register(() => RobotRaconteurNode.s.Shutdown());
app.Run("http://localhost:5000");

class GreeterImpl : experimental.aspnet_example.Greeter_default_impl
{
    string greeting = "Hello";

    public override Task<string> get_greeting(System.Threading.CancellationToken cancel = default)
    {
        return Task.FromResult(greeting);
    }

    public override Task set_greeting(string value, System.Threading.CancellationToken cancel = default)
    {
        greeting = value;
        return Task.CompletedTask;
    }

    public override Task<string> say_hello(string name, System.Threading.CancellationToken cancel = default)
    {
        return Task.FromResult($"{greeting}, {name}!");
    }
}
