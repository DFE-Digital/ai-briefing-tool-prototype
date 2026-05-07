namespace BriefingTool.Services.Interfaces;

public interface ITokenService
{
    Task<string> GetAccessTokenAsync();
}
