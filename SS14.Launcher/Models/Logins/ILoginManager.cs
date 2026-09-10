using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DynamicData;
using SS14.Launcher.Api;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher.Models.Logins;

/// <summary>
/// Service interface for account authentication, token refresh, and login management.
/// </summary>
public interface ILoginManager : INotifyPropertyChanged
{
    Guid? ActiveAccountId { get; set; }
    LoggedInAccount? ActiveAccount { get; }
    IObservableCache<LoggedInAccount, Guid> Logins { get; }

    Task Initialize();
    void AddFreshLogin(LoginInfo info);
    void UpdateToNewToken(LoggedInAccount account, LoginToken token);
    Task UpdateSingleAccountStatus(LoggedInAccount account);
}
