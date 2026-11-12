using System.Net;
using DeskMate.Core;
using DeskMate.Core.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeskMate.Transport.SignalR;

/// <summary>Builds the local control server. Binds to the loopback interface only.</summary>
public static class DeskMateControlServer
{
    /// <param name="port">TCP port on 127.0.0.1 (0 = pick a free port).</param>
    /// <param name="createPuppet">Creates the puppet once the event sink (which needs the hub) is available.</param>
    public static WebApplication Build(int port, Func<IPuppetEventSink, Puppet> createPuppet, Action<ILoggingBuilder>? logging = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, port));
        if (logging is not null)
            logging(builder.Logging);

        builder.Services.AddSignalR(o =>
        {
            o.MaximumReceiveMessageSize = 64 * 1024;
            o.EnableDetailedErrors = false;
        });
        builder.Services.AddSingleton<IPuppetEventSink, SignalREventSink>();
        builder.Services.AddSingleton(sp => createPuppet(sp.GetRequiredService<IPuppetEventSink>()));

        var app = builder.Build();
        app.MapGet("/", (Puppet puppet) => Results.Json(new
        {
            name = "DeskMate",
            protocolVersion = ProtocolInfo.Version,
            hub = ProtocolInfo.HubPath,
            character = puppet.Character.Id,
        }));
        app.MapHub<DeskMateHub>(ProtocolInfo.HubPath);
        return app;
    }
}
