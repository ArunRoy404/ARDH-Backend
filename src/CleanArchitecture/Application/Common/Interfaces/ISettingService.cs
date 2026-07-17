using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models.Setting;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface ISettingService
{
    Task<SettingViewModel> Get(CancellationToken cancellationToken);
    Task Update(SettingUpdateRequest request, CancellationToken cancellationToken);
}
