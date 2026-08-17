namespace CleanArchitecture.Application.Common.Utilities;

public static class EmailTemplateBuilder
{
    private const string WebsiteUrl = "https://ardh.co.in/";
    private const string DefaultCompanyName = "Ardh Property Management";

    public static string Build(string? logoUrl, string? companyName, string bodyContentHtml)
    {
        var displayName = string.IsNullOrWhiteSpace(companyName) ? DefaultCompanyName : companyName;

        var headerHtml = string.IsNullOrWhiteSpace(logoUrl)
            ? $@"<span style=""font-size:20px;font-weight:700;color:#111827;letter-spacing:0.2px;"">{displayName}</span>"
            : $@"<img src=""{logoUrl}"" alt=""{displayName}"" style=""max-height:42px;max-width:200px;display:inline-block;"" />";

        return $@"
<div style=""background:#f3f4f6;padding:40px 16px;font-family:'Segoe UI',Helvetica,Arial,sans-serif;"">
  <div style=""max-width:520px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 2px 10px rgba(17,24,39,0.06);"">
    <div style=""padding:28px 32px;text-align:center;border-bottom:1px solid #f1f1f4;background:#fafafa;"">
      {headerHtml}
    </div>
    <div style=""padding:36px 32px;"">
      {bodyContentHtml}
    </div>
    <div style=""padding:22px 32px;text-align:center;background:#fafafa;border-top:1px solid #f1f1f4;"">
      <a href=""{WebsiteUrl}"" style=""color:#4f46e5;font-size:13px;font-weight:600;text-decoration:none;"">{WebsiteUrl}</a>
      <p style=""margin:10px 0 0;color:#9ca3af;font-size:11px;"">&copy; {DateTime.UtcNow.Year} {displayName}. All rights reserved.</p>
    </div>
  </div>
</div>";
    }
}
