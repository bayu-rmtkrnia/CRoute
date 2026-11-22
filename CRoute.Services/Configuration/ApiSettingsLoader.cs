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
    /// Memuat konfigurasi dari appsettings.json di folder output aplikasi,
    /// lalu appsettings.local.json (jika ada), lalu environment variable
    /// berawalan <c>CROUTE_</c>. Contoh: <c>CROUTE_Database__ConnectionString</c>.
    /// </summary>
    public static ApiSettings Load(string? basePath = null)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath ?? AppContext.BaseDirectory)
            .AddJsonFile(DefaultFileName, optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("CROUTE_")
            .Build();

        var settings = new ApiSettings();
        configuration.Bind(settings);

        // Placeholder di appsettings.json jangan sampai dipakai sebagai
        // connection string. Kosongkan supaya App.xaml.cs menampilkan pesan
        // "connection string belum diatur" yang jelas.
        if (settings.Database.ConnectionString.StartsWith("GANTI_DENGAN", StringComparison.Ordinal))
            settings.Database.ConnectionString = string.Empty;

        return settings;
    }
}
