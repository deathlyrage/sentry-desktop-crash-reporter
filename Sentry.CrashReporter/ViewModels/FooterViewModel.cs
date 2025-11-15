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
            await _reporter.SubmitAsync(_envelope!);

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
        var feedback = _reporter.Feedback;

        // Subject line for Zendesk ticket
        var subject = $"Crash report {ShortEventId ?? EventId}";

        // Build description from feedback + crash info
        var descriptionBuilder = new System.Text.StringBuilder();

        if (!string.IsNullOrWhiteSpace(feedback?.Message))
        {
            descriptionBuilder.AppendLine(feedback.Message);
            descriptionBuilder.AppendLine();
        }

        descriptionBuilder.AppendLine($"Event ID: {EventId}");

        if (!string.IsNullOrWhiteSpace(Dsn))
            descriptionBuilder.AppendLine($"DSN: {Dsn}");

        if (!string.IsNullOrWhiteSpace(feedback?.Name))
            descriptionBuilder.AppendLine($"Username: {feedback.Name}");

        if (!string.IsNullOrWhiteSpace(feedback?.Email))
            descriptionBuilder.AppendLine($"Email: {feedback.Email}");

        // === Zendesk URL setup ===
        // Change these to match your Zendesk instance / form:
        const string baseUrl = "https://support.alderongames.com/hc/en-us/requests/new";
        const string ticketFormId = "900001271243"; // your Zendesk ticket form id

        static string Encode(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : System.Uri.EscapeDataString(value);

        var description = descriptionBuilder.ToString();

        var query = $"?ticket_form_id={ticketFormId}" +
                    $"&tf_subject={Encode(subject)}" +
                    $"&tf_description={Encode(description)}";

        // Prefill requester email if present
        if (!string.IsNullOrWhiteSpace(feedback?.Email))
        {
            query += $"&tf_requester_email={Encode(feedback!.Email)}";
        }

        var uri = new System.Uri(baseUrl + query);

        // Opens default browser to the Zendesk form
        await Launcher.LaunchUriAsync(uri);
    }


    [ReactiveCommand]
    private void Cancel() => _window.Close();
}
