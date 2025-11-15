using System.Buffers.Text;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public enum FooterStatus
{
    Empty,
    Normal,
    Busy,
    Error
}

public partial class FooterViewModel : ReactiveObject
{
    private readonly ICrashReporter _reporter;
    private readonly IWindowService _window;
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private string? _dsn = string.Empty;
    [ObservableAsProperty] private string? _eventId = string.Empty;
    [ObservableAsProperty] private string? _shortEventId = string.Empty;
    [ObservableAsProperty] private bool _isSubmitting;
    private readonly IObservable<bool> _canSubmit;
    [Reactive] private string? _errorMessage;
    [ObservableAsProperty] private FooterStatus _status;
    [Reactive] private bool _createZendeskTicket;

    public FooterViewModel(ICrashReporter? reporter = null, IWindowService? windowService = null)
    {
        _reporter = reporter ?? App.Services.GetRequiredService<ICrashReporter>();
        _window = windowService ?? App.Services.GetRequiredService<IWindowService>();

        _dsnHelper = this.WhenAnyValue(x => x.Envelope,  e => e?.TryGetDsn())
            .ToProperty(this, x => x.Dsn);

        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _shortEventIdHelper = this.WhenAnyValue(x => x.EventId)
            .Select(eventId => eventId?.Replace("-", string.Empty)[..8])
            .ToProperty(this, x => x.ShortEventId);

        _canSubmit = this.WhenAnyValue(x => x.Dsn, dsn => !string.IsNullOrWhiteSpace(dsn));

        _isSubmittingHelper = this.WhenAnyObservable(x => x.SubmitCommand.IsExecuting)
            .ToProperty(this, x => x.IsSubmitting);

        _statusHelper = this.WhenAnyValue(
                x => x.EventId,
                x => x.IsSubmitting,
                x => x.ErrorMessage,
                (eventId, isSubmitting, errorMessage) =>
                {
                    if (isSubmitting) return FooterStatus.Busy;
                    if (!string.IsNullOrEmpty(errorMessage)) return FooterStatus.Error;
                    if (!string.IsNullOrEmpty(eventId)) return FooterStatus.Normal;
                    return FooterStatus.Empty;
                })
            .ToProperty(this, x => x.Status);
    }

    [ReactiveCommand(CanExecute = nameof(_canSubmit))]
    private async Task Submit()
    {
        ErrorMessage = null;
        try
        {
            //await _reporter.SubmitAsync(_envelope!);

            // NEW: optionally create a Zendesk ticket
            if (CreateZendeskTicket)
            {
                await LaunchZendeskTicketAsync();
            }

            _window.Close();
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
        }
    }

    private async Task LaunchZendeskTicketAsync()
    {
        const string BaseUrl = "https://support.alderongames.com/hc/en-us/requests/new";
        const string TicketFormId = "900001271243";

        // Field IDs from your form
        const string PlatformFieldId = "900008407706";
        const string UsernameFieldId = "900008408186";
        const string AlderonIdFieldId = "900008407386";

        static string Enc(string? v) => Uri.EscapeDataString(v ?? "");

        var fb = _reporter.Feedback;

        // Map OS → Zendesk accepted values
        string platformValue =
            OperatingSystem.IsWindows() ? "pot_os_windows" :
            OperatingSystem.IsMacOS() ? "pot_os_macos" :    // confirm exact value
            OperatingSystem.IsLinux() ? "pot_os_linux" :    // confirm exact value
                                          "pot_os_windows";    // fallback

        // Subject line
        var subject = $"Crash report {ShortEventId ?? EventId}";

        // Description includes crash metadata + user message
        var description =
            $"{fb?.Message}\n\n" +
            $"Event ID: {EventId}\n" +
            $"DSN: {Dsn}\n" +
            $"Platform: {platformValue}\n" +
            $"Username: {fb?.Name}\n" +
            $"Email: {fb?.Email}";

        // Build the final Zendesk URL
        var url =
            $"{BaseUrl}?" +
            $"ticket_form_id={TicketFormId}" +
            $"&tf_anonymous_requester_email={Enc(fb?.Email)}" +
            $"&tf_subject={Enc(subject)}" +
            $"&tf_description={Enc(description)}" +
            $"&tf_{PlatformFieldId}={Enc(platformValue)}" +
            $"&tf_{UsernameFieldId}={Enc(fb?.Name)}" +
            $"&tf_{AlderonIdFieldId}="; // fill if you have it

        await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
    }


    [ReactiveCommand]
    private void Cancel() => _window.Close();
}
