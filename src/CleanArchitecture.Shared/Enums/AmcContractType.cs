using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<AmcContractType>))]
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
