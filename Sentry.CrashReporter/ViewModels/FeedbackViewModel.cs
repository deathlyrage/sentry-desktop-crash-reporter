using Sentry.CrashReporter.Services;
using System.Reactive.Linq;
using System.Text.Json.Nodes;
using Sentry.CrashReporter.Extensions;
namespace Sentry.CrashReporter.ViewModels;

public partial class FeedbackViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private string? _dsn;
    [ObservableAsProperty] private string? _eventId;
    [ObservableAsProperty] private bool _isAvailable;
    [ObservableAsProperty] private bool _isEnabled;
    [Reactive] private string _message = string.Empty;
    [Reactive] private string? _email;
    [Reactive] private string? _name;
    [Reactive] private string? _userId;

    public FeedbackViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= App.Services.GetRequiredService<ICrashReporter>();
        if (reporter.Feedback != null)
        {
            Name = reporter.Feedback.Name;
            Email = reporter.Feedback.Email;
            Message = reporter.Feedback.Message;
            UserId = reporter.Feedback.UserId;
        }

        // NEW: when an Envelope arrives, read user.username + user.email once
        this.WhenAnyValue(x => x.Envelope)
            .Where(e => e is not null)
            .Take(1) // only need to initialize once
            .Subscribe(env =>
            {
                var evt = env!.TryGetEvent();
                var payload = evt?.TryParseAsJson();
                var user = payload?.TryGetProperty("user")?.AsObject();

                if (user is null)
                    return;

                // Only overwrite if not already set (e.g. from reporter.Feedback)
                if (string.IsNullOrWhiteSpace(Name))
                    Name = user.TryGetString("username");

                if (string.IsNullOrWhiteSpace(UserId))
                    UserId = user.TryGetString("id");

                if (string.IsNullOrWhiteSpace(Email))
                    Email = user.TryGetString("email");
            });

        _dsnHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetDsn())
            .ToProperty(this, x => x.Dsn);

        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _isAvailableHelper = this.WhenAnyValue(x => x.Dsn, y => y.EventId, (x, y) => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(y))
            .ToProperty(this, x => x.IsAvailable);

        _isEnabledHelper = this.WhenAnyValue(x => x.IsAvailable, y => y.Message, (a, m) => a && !string.IsNullOrWhiteSpace(m))
            .ToProperty(this, x => x.IsEnabled);

        this.WhenAnyValue(x => x.Name, x => x.Email, x => x.UserId, x => x.Message)
            .Subscribe(_ => reporter.UpdateFeedback(new Feedback(Name, Email, UserId, Message)));
    }
}
