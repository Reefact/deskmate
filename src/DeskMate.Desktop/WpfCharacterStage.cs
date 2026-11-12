using System.Windows.Threading;
using DeskMate.Core;
using DeskMate.Core.Ports;

namespace DeskMate.Desktop;

/// <summary>Driven adapter: implements the core's stage port on top of the WPF window, from any thread.</summary>
internal sealed class WpfCharacterStage(CharacterWindow window) : ICharacterStage
{
    private readonly Dispatcher _dispatcher = window.Dispatcher;

    public ScreenPoint FeetPosition =>
        _dispatcher.CheckAccess() ? window.GetFeetPosition() : _dispatcher.Invoke(window.GetFeetPosition);

    public void SetFeetPosition(ScreenPoint position) => Post(() => window.SetFeetPosition(position), DispatcherPriority.Render);

    public void ShowPose(string poseId) => Post(() => window.ShowPose(poseId));

    public void ShowMessage(string text) => Post(() => window.ShowMessage(text));

    public void HideMessage() => Post(window.HideMessage);

    public void SetMouthOpenness(double openness) => Post(() => window.SetMouthOpenness(openness), DispatcherPriority.Render);

    private void Post(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        if (_dispatcher.CheckAccess())
            action();
        else if (!_dispatcher.HasShutdownStarted)
            _dispatcher.BeginInvoke(action, priority);
    }
}
