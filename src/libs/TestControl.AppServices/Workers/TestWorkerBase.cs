using TestControl.Infrastructure;
using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.AppServices.Workers;

public abstract class TestWorkerBase
{
    protected bool _isActive;

    protected readonly HttpClient _httpClient;
    protected readonly TestConfig _config;
    protected readonly MessageHandler _messageHandler;
    protected readonly CancellationTokenSource _cts;
    protected readonly CancellationToken _linkedToken;

    protected Mode _mode = Mode.Fair;
    protected User _self;

    public TestWorkerBase(HttpClient httpClient,
        TestConfig config,
        MessageHandler messageHandler,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        _cts = new CancellationTokenSource();
        _linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token).Token;
        _mode = _config.Mode;
    }

    public Guid UserId => _self?.UserId ?? Guid.Empty;

    private string _name = null;

    public string Name
    {
        get
        {
            _name ??= $"{GetType().Name}-{Guid.NewGuid().ToString("N")[3..11]}";
            return _name;
        }
    }

    protected bool ShouldContinue => _isActive && !_linkedToken.IsCancellationRequested;
}
