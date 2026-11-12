using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DeskMate.Core;
using DeskMate.Core.Characters;

namespace DeskMate.Desktop;

/// <summary>
/// Borderless, transparent, topmost character window. Every member must be called on the UI thread;
/// <see cref="WpfCharacterStage"/> does the marshalling for the core.
/// </summary>
public partial class CharacterWindow : Window
{
    private const double BubbleAreaHeight = 190;
    private const double MinWindowWidth = 300;
    private const double MouthBobPixels = 2.5;

    private readonly CharacterManifest _character;
    private readonly double _scale;
    private readonly Dictionary<string, BitmapSource> _frames = new();
    private readonly TranslateTransform _speechBob = new();
    private readonly ScaleTransform _breathing = new(1, 1);
    private readonly Vector _feetOffset;

    public event Action<string, ScreenPoint>? Clicked;
    public event Action<string>? MessageSubmitted;
    public event Action? DragStarted;
    public event Action? ResetGazeRequested;

    public CharacterWindow(CharacterManifest character, string characterDirectory, double scale)
    {
        InitializeComponent();
        _character = character;
        _scale = scale;

        foreach (var pose in character.Poses)
            _frames[pose.Id] = LoadFrame(Path.Combine(characterDirectory, pose.File));

        var spriteWidth = character.FrameWidth * scale;
        var spriteHeight = character.FrameHeight * scale;
        Sprite.Width = spriteWidth;
        Sprite.Height = spriteHeight;
        BubbleRow.Height = new GridLength(BubbleAreaHeight);
        SpriteRow.Height = new GridLength(spriteHeight);
        Width = Math.Max(MinWindowWidth, spriteWidth);
        Height = BubbleAreaHeight + spriteHeight;

        // Feet anchor relative to the window's top-left corner.
        _feetOffset = new Vector(
            Width / 2 + (character.FeetAnchor.X - character.FrameWidth / 2.0) * scale,
            BubbleAreaHeight + character.FeetAnchor.Y * scale);

        Sprite.RenderTransform = new TransformGroup { Children = { _breathing, _speechBob } };
        ShowPose(character.NeutralPose);
        StartBreathing();
    }

    public ScreenPoint GetFeetPosition() => new(Left + _feetOffset.X, Top + _feetOffset.Y);

    public void SetFeetPosition(ScreenPoint feet)
    {
        Left = feet.X - _feetOffset.X;
        Top = feet.Y - _feetOffset.Y;
    }

    public void ShowPose(string poseId)
    {
        if (_frames.TryGetValue(poseId, out var frame))
            Sprite.Source = frame;
    }

    public void ShowMessage(string text)
    {
        MessageText.Text = text;
        MessageText.Visibility = Visibility.Visible;
        UpdateBubble();
    }

    public void HideMessage()
    {
        MessageText.Visibility = Visibility.Collapsed;
        MessageText.Text = "";
        UpdateBubble();
    }

    /// <summary>Placeholder lip sync until dedicated mouth sprites exist: a tiny bob driven by the mouth openness.</summary>
    public void SetMouthOpenness(double openness) =>
        _speechBob.Y = -Math.Clamp(openness, 0, 1) * MouthBobPixels;

    public void OpenInput()
    {
        InputBox.Visibility = Visibility.Visible;
        UpdateBubble();
        Activate();
        InputBox.Focus();
        Keyboard.Focus(InputBox);
    }

    private void CloseInput()
    {
        InputBox.Text = "";
        InputBox.Visibility = Visibility.Collapsed;
        UpdateBubble();
    }

    private void UpdateBubble() =>
        Bubble.Visibility = MessageText.Visibility == Visibility.Visible || InputBox.Visibility == Visibility.Visible
            ? Visibility.Visible
            : Visibility.Collapsed;

    private void StartBreathing()
    {
        var breathe = new DoubleAnimation(1.0, 1.012, TimeSpan.FromSeconds(2.2))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        Timeline.SetDesiredFrameRate(breathe, 30);
        _breathing.BeginAnimation(ScaleTransform.ScaleYProperty, breathe);
    }

    private static BitmapSource LoadFrame(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    // ---- User interaction ------------------------------------------------------------------

    private void Sprite_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (e.ClickCount == 2)
        {
            OpenInput();
            return;
        }

        var clickInWindow = e.GetPosition(this);
        var before = new Point(Left, Top);
        DragStarted?.Invoke();
        try
        {
            DragMove(); // Modal until the button is released.
        }
        catch (InvalidOperationException)
        {
            // The button was already released.
        }

        var moved = Math.Abs(Left - before.X) + Math.Abs(Top - before.Y) > 2;
        if (!moved)
            Clicked?.Invoke("left", new ScreenPoint(Left + clickInWindow.X, Top + clickInWindow.Y));
    }

    private void Sprite_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(this);
        Clicked?.Invoke("right", new ScreenPoint(Left + p.X, Top + p.Y));
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var text = InputBox.Text;
            CloseInput();
            MessageSubmitted?.Invoke(text);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseInput();
            e.Handled = true;
        }
    }

    private void WriteMessage_Click(object sender, RoutedEventArgs e) => OpenInput();

    private void ResetGaze_Click(object sender, RoutedEventArgs e) => ResetGazeRequested?.Invoke();

    private void Quit_Click(object sender, RoutedEventArgs e) => Close();
}
