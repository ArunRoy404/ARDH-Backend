namespace CleanArchitecture.Application.Common.Interfaces;

public interface ICookieService
{
    public void Set(string token, bool rememberMe = false);
    public string Get();
    public void Delete();
}
