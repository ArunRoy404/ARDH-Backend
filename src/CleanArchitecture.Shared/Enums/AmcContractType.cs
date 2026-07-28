using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AmcContractType
{
    Comprehensive,
    [JsonPropertyName("Non-Comprehensive")]
    NonComprehensive,
    [JsonPropertyName("Preventative Maintenance")]
    PreventativeMaintenance,
    [JsonPropertyName("Breakdown Maintenance")]
    BreakdownMaintenance,
    [JsonPropertyName("Operations & Maintenance")]
    OperationsAndMaintenance,
    Other
}
