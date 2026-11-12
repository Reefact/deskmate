using System.Text.Json;
using DeskMate.Core;
using DeskMate.Core.Characters;
using DeskMate.Core.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace DeskMate.Transport.SignalR.Tests;

/// <summary>End-to-end over a real loopback SignalR connection, with a fake stage instead of WPF.</summary>
public sealed class HubIntegrationTests : IAsyncLifetime
{
    private readonly FakeStage _stage = new();
    private WebApplication _app = null!;
    private HubConnection _client = null!;
    private Puppet _puppet = null!;

    public async Task InitializeAsync()
    {
        var character = CharacterManifest.Load(Path.Combine(AppContext.BaseDirectory, "Assets", "character.json"));
        _app = DeskMateControlServer.Build(0, sink =>
            _puppet = new Puppet(character, _stage, sink, new InstantSpeech(), options: new PuppetOptions { IdleEnabled = false }));
        _app.Services.GetRequiredService<Puppet>();
        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        Assert.StartsWith("http://127.0.0.1:", address);

        _client = new HubConnectionBuilder().WithUrl(address + ProtocolInfo.HubPath).Build();
        await _client.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _client.DisposeAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task GetInfo_describes_the_puppet()
    {
        var info = await _client.InvokeAsync<InfoDto>("GetInfo");

        Assert.Equal(ProtocolInfo.Version, info.ProtocolVersion);
        Assert.Equal("default", info.CharacterId);
        Assert.Equal(8, info.Poses.Count);
        Assert.True(info.CanSpeak);
    }

    [Fact]
    public async Task Move_is_accepted_then_completion_is_pushed_to_the_client()
    {
        var completed = new TaskCompletionSource<OperationCompletedEvent>();
        _client.On<OperationCompletedEvent>(ClientEvents.OperationCompleted, e => completed.TrySetResult(e));

        var accepted = await _client.InvokeAsync<OperationAccepted>("Move", new MoveRequest(200, 300, 50));
        var done = await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("move", accepted.Kind);
        Assert.Equal(accepted.OperationId, done.OperationId);
        Assert.Equal("completed", done.Status);
        Assert.Equal(new ScreenPoint(200, 300), _stage.FeetPosition);
    }

    [Fact]
    public async Task Commands_use_camelCase_json_that_any_client_can_send()
    {
        // Simulates a JavaScript/Python client sending a plain object with only some fields.
        var payload = JsonDocument.Parse("""{ "text": "Hi from JS" }""").RootElement;
        await _client.InvokeAsync("ShowMessage", payload);

        Assert.Equal("Hi from JS", _stage.Message);
        Assert.Equal("gaze-0", await _client.InvokeAsync<string>("LookAt", new LookAtRequest(1)));
    }

    [Fact]
    public async Task Invalid_commands_are_reported_as_errors()
    {
        var error = await Assert.ThrowsAsync<HubException>(() =>
            _client.InvokeAsync("ShowMessage", new ShowMessageRequest("   ")));

        Assert.Contains("must not be empty", error.Message);
    }

    [Fact]
    public async Task Speak_completes_and_user_input_is_published()
    {
        var clicked = new TaskCompletionSource<ClickedEvent>();
        var submitted = new TaskCompletionSource<MessageSubmittedEvent>();
        var spoken = new TaskCompletionSource<OperationCompletedEvent>();
        _client.On<ClickedEvent>(ClientEvents.Clicked, e => clicked.TrySetResult(e));
        _client.On<MessageSubmittedEvent>(ClientEvents.MessageSubmitted, e => submitted.TrySetResult(e));
        _client.On<OperationCompletedEvent>(ClientEvents.OperationCompleted, e => spoken.TrySetResult(e));

        await _client.InvokeAsync<OperationAccepted>("Speak", new SpeakRequest("Bonjour"));
        Assert.Equal("completed", (await spoken.Task.WaitAsync(TimeSpan.FromSeconds(5))).Status);

        _puppet.ReportClick("left", new ScreenPoint(1, 2));
        _puppet.SubmitUserMessage("hello");

        Assert.Equal(new ClickedEvent("left", 1, 2), await clicked.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal("hello", (await submitted.Task.WaitAsync(TimeSpan.FromSeconds(5))).Text);
    }

    private sealed class InstantSpeech : ISpeechSynthesizer
    {
        public Task SpeakAsync(string text, Action<SpeechViseme> onViseme, CancellationToken cancellationToken)
        {
            onViseme(new SpeechViseme(2, TimeSpan.FromMilliseconds(50)));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStage : ICharacterStage
    {
        private ScreenPoint _position;
        public string? Message { get; private set; }
        public ScreenPoint FeetPosition => _position;
        public void SetFeetPosition(ScreenPoint position) => _position = position;
        public void ShowPose(string poseId) { }
        public void ShowMessage(string text) => Message = text;
        public void HideMessage() => Message = null;
        public void SetMouthOpenness(double openness) { }
    }
}
