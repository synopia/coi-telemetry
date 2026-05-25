using System.Threading;
using CoiTelemetry.RealMod.Contracts.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace CoiTelemetry.RealMod.Web;

public sealed class LiveDataHub
{
    private string _latestSummaryJson = "{}";
    private long _version;
    public string? RequestEntity { get; set; }
    public string? ResponseEntity { get; set; }

    private static readonly JsonSerializerSettings PrettySettings = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
    };

    public void UpdateLatest(LiveSummary summary)
    {
        var json = JsonConvert.SerializeObject(summary, PrettySettings);
        Interlocked.Increment(ref _version);
        Volatile.Write(ref _latestSummaryJson, json);
    }

    public LiveDataSnapshot GetLatest()
    {
        return new LiveDataSnapshot(
            Version: Interlocked.Read(ref _version),
            Json: Volatile.Read(ref _latestSummaryJson)
            );
    }
}

public sealed record LiveDataSnapshot(
    long Version,
    string Json
);