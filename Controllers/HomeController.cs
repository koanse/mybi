using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Highsoft.Web.Mvc.Charts;
using Microsoft.AspNetCore.Mvc;
using mybi.Models;

namespace mybi.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var company = "IBM";
        var apiKey = "demo"; // replace the "demo" apikey below with your own key from https://www.alphavantage.co/support/#api-key
        var queryUri = new Uri($"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={company}&apikey={apiKey}");
        var seriesName = "Time Series (Daily)";
        var seriesValueName = new List<string>() { "1. open", "2. high", "3. low", "4. close" };

        Highcharts chartOptions;
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(queryUri);
            var jsonStream = await response.Content.ReadAsStreamAsync();
            var data = (await JsonSerializer.DeserializeAsync<Dictionary<string, JsonObject>>(jsonStream))![seriesName];
            var chartData = seriesValueName.ToDictionary(x => x, x => new List<ColumnSeriesData>());
            var categories = new List<string>();
            data.ToList().ForEach(d =>
            {
                categories.Add(d.Key);
                seriesValueName.ForEach(x => chartData[x].Add(new() { Y = double.Parse(d.Value[x].GetValue<string>()) }));
            });

            chartOptions = new Highcharts
            {
                ID = "chart",
                Title = new Title { Text = $"{seriesName}. {company}. NYSE Delayed Price. Currency in USD" },
                Subtitle = new Subtitle { Text = "Source: AlphaVantage.co" },
                XAxis = [new() { Categories = categories }],
                YAxis = [new() { Min = 0, Title = new() { Text = "NYSE Delayed Price, USD" } }],
                Tooltip = new Tooltip
                {
                    HeaderFormat = "<span style='font-size:10px'>{point.key}</span><table style='font-size:12px'>",
                    PointFormat = "<tr><td style='color:{series.color};padding:0'>{series.name}: </td><td style='padding:0'><b>{point.y:.1f} mm</b></td></tr>",
                    FooterFormat = "</table>",
                    Shared = true,
                    UseHTML = true
                },
                PlotOptions = new PlotOptions
                {
                    Column = new PlotOptionsColumn
                    {
                        PointPadding = 0.2,
                        BorderWidth = 0
                    }
                },
                Series = chartData.Select(x => new ColumnSeries { Name = x.Key, Data = x.Value }).Cast<Series>().ToList()
            };
        }
        catch (Exception ex)
        {
            chartOptions = new Highcharts
            {
                ID = "chart",
                Title = new() { Text = $"{seriesName}. {company}. NYSE Delayed Price. Currency in USD." },
                Subtitle = new() { Text = $"Source: AlphaVantage.co. Error occured: {ex.Message}" }
            };
        }

        ViewData["chartOptions"] = chartOptions;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
