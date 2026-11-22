using CRoute.Services.Configuration;

namespace CRoute.Tests.Services;

public sealed class ApiSettingsLoaderTests : IDisposable
{
    private static readonly string[] EnvironmentKeys =
    [
        "CROUTE_GoogleMaps__ApiKey",
        "CROUTE_Climatiq__ApiKey",
        "CROUTE_Database__ConnectionString",
    ];

    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "croute-tests",
        Guid.NewGuid().ToString("N"));

    private readonly Dictionary<string, string?> _savedEnvironment = new();

    public ApiSettingsLoaderTests()
    {
        foreach (string key in EnvironmentKeys)
        {
            _savedEnvironment[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, null);
        }

        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        foreach ((string key, string? value) in _savedEnvironment)
            Environment.SetEnvironmentVariable(key, value);

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private void WriteFile(string name, string json)
        => File.WriteAllText(Path.Combine(_directory, name), json);

    [Fact]
    public void Load_MembacaFileUtama()
    {
        WriteFile("appsettings.json", """
            {
              "GoogleMaps": {
                "ApiKey": "main-key",
                "DistanceMatrixBaseUrl": "https://maps.test/distancematrix"
              },
              "Database": {
                "ConnectionString": "Host=db.test"
              }
            }
            """);

        ApiSettings settings = ApiSettingsLoader.Load(_directory);

        Assert.Equal("main-key", settings.GoogleMaps.ApiKey);
        Assert.Equal("https://maps.test/distancematrix", settings.GoogleMaps.DistanceMatrixBaseUrl);
        Assert.Equal("Host=db.test", settings.Database.ConnectionString);
    }

    [Fact]
    public void Load_FileLokal_MenimpaFileUtama()
    {
        WriteFile("appsettings.json", """
            {
              "GoogleMaps": {
                "ApiKey": "main-key",
                "DistanceMatrixBaseUrl": "https://maps.test/distancematrix"
              }
            }
            """);
        WriteFile("appsettings.local.json", """
            {
              "GoogleMaps": {
                "ApiKey": "local-key"
              }
            }
            """);

        ApiSettings settings = ApiSettingsLoader.Load(_directory);

        Assert.Equal("local-key", settings.GoogleMaps.ApiKey);
        Assert.Equal("https://maps.test/distancematrix", settings.GoogleMaps.DistanceMatrixBaseUrl);
    }

    [Fact]
    public void Load_ConnectionStringPlaceholder_Dikosongkan()
    {
        WriteFile("appsettings.json", """
            {
              "Database": {
                "ConnectionString": "GANTI_DENGAN_CONNECTION_STRING"
              }
            }
            """);

        ApiSettings settings = ApiSettingsLoader.Load(_directory);

        Assert.Equal(string.Empty, settings.Database.ConnectionString);
    }

    [Fact]
    public void Load_EnvironmentVariable_MenimpaSemuaFile()
    {
        WriteFile("appsettings.json", """
            {
              "GoogleMaps": {
                "ApiKey": "main-key"
              }
            }
            """);
        WriteFile("appsettings.local.json", """
            {
              "GoogleMaps": {
                "ApiKey": "local-key"
              }
            }
            """);
        Environment.SetEnvironmentVariable("CROUTE_GoogleMaps__ApiKey", "env-key");

        ApiSettings settings = ApiSettingsLoader.Load(_directory);

        Assert.Equal("env-key", settings.GoogleMaps.ApiKey);
    }
}
