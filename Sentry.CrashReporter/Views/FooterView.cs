using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class FooterView : ReactiveUserControl<FooterViewModel>
{
    public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
        nameof(Envelope), typeof(Envelope), typeof(FooterView), new PropertyMetadata(null));

    public Envelope? Envelope
    {
        get => (Envelope)GetValue(EnvelopeProperty);
        set => SetValue(EnvelopeProperty, value);
    }

    public FooterView()
    {
        ViewModel = new FooterViewModel();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(v => v.Envelope)
                .BindTo(ViewModel, vm => vm.Envelope)
                .DisposeWith(d);
        });

        this.Content(new Grid()
            .DataContext(ViewModel, (view, vm) => view
                .ColumnSpacing(8)
                .ColumnDefinitions("Auto,*,Auto,Auto")
                .Children(

                    // Column 0: Crash Id / status
                    new ContentControl()
                        .Grid(0)
                        .Content(x => x.Binding(() => vm.Status)
                            .Convert(status => BuildStatusLabel(vm, status).Name("statusLabel"))),

                    // Column 1: checkbox, centered in the middle area
                    new CheckBox()
                        .Grid(1)
                        .Content("I need help, create a support ticket.")
                        .VerticalAlignment(VerticalAlignment.Center)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .IsChecked(x => x.Binding(() => vm.CreateZendeskTicket).TwoWay()),

                    // Column 2: Cancel button
                    new Button()
                        .Grid(2)
                        .Content("Cancel")
                        .Name("cancelButton")
                        .Command(x => x.Binding(() => vm.CancelCommand))
                        .Background(Colors.Transparent),

                // Column 3: Submit button
                new Button()
                    .Grid(3)
                    .Content("Submit")
                    .Name("submitButton")
                    .AutomationProperties(automationId: "submitButton")
                    .Command(x => x.Binding(() => vm.SubmitCommand))
                    .Style(StaticResource.Get<Style>("AccentButtonStyle"))
                    .CornerRadius(ThemeResource.Get<CornerRadius>("ControlCornerRadius"))))
            );
    }

    FrameworkElement BuildStatusLabel(FooterViewModel vm, FooterStatus status)
    {
        return status switch
        {
            FooterStatus.Normal => new IconLabel(FA.Copy)
                .ToolTip("Event ID")
                .Text(x => x.Binding(() => vm.EventId)
                    .Convert(id => string.IsNullOrWhiteSpace(id)
                        ? string.Empty
                        : $"Crash Id: {id}")),

            FooterStatus.Busy => new IconLabel()
                .Icon(new ProgressRing()
                    .IsActive(true)
                    .Width(20)
                    .Height(20))
                .IsTextSelectionEnabled(false)
                .Text("Please wait. Submitting the report..."),

            FooterStatus.Error => new IconLabel(FA.CircleExclamation)
                .TextWrapping(TextWrapping.Wrap)
                .VerticalAlignment(VerticalAlignment.Center)
                .Text(x => x.Binding(() => vm.ErrorMessage))
                .Foreground(ThemeResource.Get<Brush>("SystemErrorTextColor")),
            _ => new Control()
                .Visibility(Visibility.Collapsed),
        };
    }
}
