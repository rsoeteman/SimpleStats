using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace SimpleStats.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IHttpResponseFeature"/> that records <see cref="OnStarting"/> callbacks
/// instead of firing them (that's normally the server's job), so a test can trigger them manually
/// via <see cref="FireOnStartingAsync"/> to exercise code registered with
/// <c>HttpResponse.OnStarting</c>.
/// </summary>
internal sealed class RecordingHttpResponseFeature : IHttpResponseFeature
{
    private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

    public int StatusCode { get; set; } = 200;

    public string? ReasonPhrase { get; set; }

    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

    public Stream Body { get; set; } = Stream.Null;

    public bool HasStarted { get; private set; }

    public void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));

    public void OnCompleted(Func<object, Task> callback, object state)
    {
    }

    public async Task FireOnStartingAsync()
    {
        HasStarted = true;
        foreach ((Func<object, Task> callback, object state) in _onStarting)
        {
            await callback(state);
        }
    }
}
