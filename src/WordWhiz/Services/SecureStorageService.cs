using System.Security.Cryptography;
using System.Text;
using WordWhiz.Services.Interfaces;

namespace WordWhiz.Services;

/// <summary>
/// Secure storage using Windows DPAPI (Data Protection API).
/// Encrypts data bound to the current Windows user account.
/// </summary>
public class SecureStorageService : ISecureStorageService
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("WordWhiz.v1.SecureStorage");
    private readonly IDataService _dataService;

    public SecureStorageService(IDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task StoreAsync(string key, string value)
    {
        var plainBytes = Encoding.UTF8.GetBytes(value);
        var encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        var base64 = Convert.ToBase64String(encryptedBytes);
        await _dataService.SetSettingAsync($"secure_{key}", base64);
    }

    public async Task<string?> RetrieveAsync(string key)
    {
        var base64 = await _dataService.GetSettingAsync<string?>($"secure_{key}", null);
        if (base64 == null) return null;

        try
        {
            var encryptedBytes = Convert.FromBase64String(base64);
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key)
    {
        await _dataService.SetSettingAsync<string?>($"secure_{key}", null);
    }

    public async Task<bool> ContainsKeyAsync(string key)
    {
        var value = await RetrieveAsync(key);
        return value != null;
    }
}

/// <summary>
/// Interface for secure storage service.
/// </summary>
public interface ISecureStorageService
{
    Task StoreAsync(string key, string value);
    Task<string?> RetrieveAsync(string key);
    Task DeleteAsync(string key);
    Task<bool> ContainsKeyAsync(string key);
}
