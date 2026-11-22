using CRoute.Data;

namespace CRoute.Tests.Data;

public class PostgresAuthServiceTests
{
    // Guard validasi berjalan sebelum koneksi database dibuka, jadi cukup
    // connection string dummy. Kalau guard tidak ada, test akan mencoba
    // koneksi dan gagal dengan error yang berbeda.
    private const string DummyConnectionString =
        "Host=localhost;Port=5432;Database=croute_test;Username=test;Password=test";

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    public void Register_PasswordKurangDariEnam_MelemparArgumentException(string password)
    {
        var service = new PostgresAuthService(DummyConnectionString);

        Assert.Throws<ArgumentException>(
            () => service.Register("Nama Uji", "uji_password", password));
    }
}
