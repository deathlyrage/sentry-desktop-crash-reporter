using CommunityToolkit.WinUI.Converters;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class HeaderView : ReactiveUserControl<HeaderViewModel>
{
    public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
        nameof(Envelope), typeof(Envelope), typeof(HeaderView), new PropertyMetadata(null));

    public Envelope? Envelope
    {
        get => (Envelope)GetValue(EnvelopeProperty);
        set => SetValue(EnvelopeProperty, value);
    }

    private static readonly StringVisibilityConverter ToVisibility = new();

    public HeaderView()
    {
        ViewModel = new HeaderViewModel();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(v => v.Envelope)
                .BindTo(ViewModel, vm => vm.Envelope)
                .DisposeWith(d);
        });

        this.Content(new Grid()
            .DataContext(ViewModel, (view, vm) => view
                // Column 0 = logo, Column 1 = text
                .ColumnDefinitions("Auto,*")
                .Children(

                    // LOGO — using the theme resource image source
                    new Image()
                        .Grid(0)
                        .Source(ThemeResource.Get<ImageSource>("SentryGlyphIcon"))
                        .Width(128)
                        .Height(128)
                        .HorizontalAlignment(HorizontalAlignment.Left)
                        .VerticalAlignment(VerticalAlignment.Top)
                        .Margin(0, 0, 16, 0),

                    // TEXT AREA — now in Column 1
                    new StackPanel()
                        .Grid(1)
                        .Orientation(Orientation.Vertical)
                        .Spacing(8)
                        .Children(
                            new TextBlock()
                                .Text("Report a Crash")
                                .Style(ThemeResource.Get<Style>("TitleTextBlockStyle")),

                            new TextBlock()
                                .Text("We’re constantly fixing crashes to keep the game running smoothly.")
                                .TextWrapping(TextWrapping.Wrap),

                            new TextBlock()
                                .Text("If this one is urgent, please check the open a support ticket to get help after sending in the crash.")
                                .TextWrapping(TextWrapping.Wrap),

                            new TextBlock()
                                .TextWrapping(TextWrapping.Wrap)
                                .Inlines(
                                    new Run { Text = "If not, a bug report really helps us out. " },
                                    new Hyperlink
                                    {
                                        NavigateUri = new Uri("https://bugtracker.alderongames.com/bug/create"),
                                        Inlines = { new Run { Text = "Open Bug Tracker" } }
                                    }
                                )
                                .Margin(0, 0, 0, 8),

                            new WrapPanel()
                                .Margin(-4, 0)
                                .Orientation(Orientation.Horizontal)
                                .Children(
                                    new IconLabel(FA.Bug)
                                        .Margin(8, 4)
                                        .Name("exceptionLabel")
                                        .ToolTip(b => b.ToolTip(() => vm.Exception, e => e?.Value ?? "Exception"))
                                        .Text(x => x.Binding(() => vm.Exception).Convert(e => e?.Type ?? string.Empty))
                                        .Visibility(x => x.Binding(() => vm.Exception)
                                            .Convert(e => string.IsNullOrEmpty(e?.Type) ? Visibility.Collapsed : Visibility.Visible)),

                                    new IconLabel(FA.Globe)
                                        .Margin(8, 4)
                                        .Name("releaseLabel")
                                        .ToolTip("Release")
                                        .Text(x => x.Binding(() => vm.Release))
                                        .Visibility(x => x.Binding(() => vm.Release).Converter(ToVisibility)),

                                    new IconLabel()
                                        .Margin(8, 4)
                                        .Name("osLabel")
                                        .Brand(x => x.Binding(() => vm.OsName).Convert(ToBrand))
                                        .ToolTip("Operating System")
                                        .Text(x => x.Binding(() => vm.OsPretty))
                                        .Visibility(x => x.Binding(() => vm.OsPretty).Converter(ToVisibility)),

                                    new IconLabel(FA.Wrench)
                                        .Margin(8, 4)
                                        .Name("environmentLabel")
                                        .ToolTip("Environment")
                                        .Text(x => x.Binding(() => vm.Environment))
                                        .Visibility(x => x.Binding(() => vm.Environment).Converter(ToVisibility))
                                )
                        )
                )
            )
        );
    }

    private static string ToBrand(string? value)
    {
        return value?.ToLower() switch
        {
            "android" => FA.Android,
            "linux" => FA.Linux,
            "windows" => FA.Windows,
            "apple" or "macos" or "ios" or "tvos" or "visionos" or "watchos" => FA.Apple,
            _ => string.Empty
        };
    }
}
