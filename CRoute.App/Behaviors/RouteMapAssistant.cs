using System.IO;
using System.Text.Json;
using System.Windows;
using CRoute.Core.Models;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace CRoute.App.Behaviors;

/// <summary>
/// Menjembatani <see cref="Route"/> di ViewModel ke peta Leaflet di dalam
/// <see cref="WebView2"/>. WebView2 tidak punya property yang bisa di-binding
/// untuk "isi halaman", jadi attached property ini yang mengirim data rute ke
/// JavaScript: <c>behaviors:RouteMapAssistant.Route="{Binding MapRoute}"</c>.
/// </summary>
/// <remarks>
/// Halaman peta (<c>Assets/Map/map.html</c> + Leaflet) dimuat lewat host
/// virtual <c>https://croute.local/</c>, bukan <c>NavigateToString</c>, supaya
/// request tile OpenStreetMap punya Referer (diwajibkan kebijakan tile OSM).
/// </remarks>
public static class RouteMapAssistant
{
    private const string HostName = "croute.local";
    private const string MapUrl = "https://" + HostName + "/map.html";

    public static readonly DependencyProperty RouteProperty =
        DependencyProperty.RegisterAttached(
            "Route",
            typeof(Route),
            typeof(RouteMapAssistant),
            new PropertyMetadata(null, OnRouteChanged));

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached("State", typeof(MapState), typeof(RouteMapAssistant));

    public static Route? GetRoute(DependencyObject d) => (Route?)d.GetValue(RouteProperty);

    public static void SetRoute(DependencyObject d, Route? value) => d.SetValue(RouteProperty, value);

    private static void OnRouteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WebView2 view)
            return;

        if (view.GetValue(StateProperty) is not MapState state)
        {
            state = new MapState();
            view.SetValue(StateProperty, state);
            _ = InitializeAsync(view, state);
            return;
        }

        if (state.IsReady)
            PostRoute(view);
    }

    private static async Task InitializeAsync(WebView2 view, MapState state)
    {
        try
        {
            // Data WebView2 (cache tile, cookie) disimpan di LocalAppData,
            // bukan di folder bin di samping .exe.
            string userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CRoute",
                "WebView2");
            CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userData);
            await view.EnsureCoreWebView2Async(env);

            string mapFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "Map");
            view.CoreWebView2.SetVirtualHostNameToFolderMapping(
                HostName, mapFolder, CoreWebView2HostResourceAccessKind.Allow);

            view.CoreWebView2.Settings.AreDevToolsEnabled = false;
            view.CoreWebView2.Settings.IsStatusBarEnabled = false;
            view.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

            view.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                state.IsReady = args.IsSuccess;
                if (state.IsReady)
                    PostRoute(view);
            };

            view.CoreWebView2.Navigate(MapUrl);
        }
        catch (Exception ex) when (ex is WebView2RuntimeNotFoundException or InvalidOperationException or IOException)
        {
            // Peta hanya pelengkap: kalau WebView2 Runtime tidak ada, hasil
            // perhitungan emisi tetap tampil tanpa peta.
            view.Visibility = Visibility.Collapsed;
        }
    }

    private static void PostRoute(WebView2 view)
    {
        Route? route = GetRoute(view);
        string json = JsonSerializer.Serialize(new
        {
            polyline = route?.EncodedPolyline,
            origin = route?.Origin,
            destination = route?.Destination,
        });

        view.CoreWebView2?.PostWebMessageAsJson(json);
    }

    private sealed class MapState
    {
        public bool IsReady { get; set; }
    }
}
