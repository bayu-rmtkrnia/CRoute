using Microsoft.Extensions.Configuration;

namespace CRoute.Services.Configuration;

/// <summary>
/// Pembaca appsettings.json. Dipisah dari service supaya service-nya sendiri
/// cuma bergantung pada <see cref="ApiSettings"/> dan gampang di-test.
/// </summary>
public static class ApiSettingsLoader
{
    public const string DefaultFileName = "appsettings.json";

    /// <summary>
    /// Memuat konfigurasi dari appsettings.json di folder output aplikasi.
    /// File yang tidak ada dianggap konfigurasi kosong, jadi aplikasi tetap
    /// bisa jalan dengan implementasi fake selama backend belum dipakai.
    /// </summary>
    public static ApiSettings Load(string? basePath = null)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath ?? AppContext.BaseDirectory)
            .AddJsonFile(DefaultFileName, optional: true, reloadOnChange: false)
            .Build();

        var settings = new ApiSettings();
        configuration.Bind(settings);

        return settings;
    }
}
