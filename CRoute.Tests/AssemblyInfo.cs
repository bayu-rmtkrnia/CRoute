using Xunit;

// Test konfigurasi mengubah environment variable proses, jadi test tidak boleh
// berjalan paralel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
