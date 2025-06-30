using LifxCloud.NET.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MijnThuis.Application.DependencyInjection;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;
using System;
using System.Globalization;

Console.WriteLine("MijnThuis console tool");
Console.WriteLine("----------------------");
Console.WriteLine();

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddApplication();
serviceCollection.AddSingleton<IConfiguration>(configuration);
using var serviceProvider = serviceCollection.BuildServiceProvider();

//var flagRepository = serviceProvider.GetRequiredService<IFlagRepository>();
//await flagRepository.SetElectricityTariffDetailsFlag(fixedCharge: 42.4M, greenEnergyContribution: 1.554M, capacityTariff: 53.2565412M, usageTariff: 5.99007M, dataAdministration: 18.56M, specialExciseTax: 5.03288M, energyContribution: 0.20417M);
//await flagRepository.SetSamsungTheFrameTokenFlag("", new TimeSpan(8, 0, 0), new TimeSpan(22, 0, 0));
//await flagRepository.SetConsumptionTariffExpressionFlag("price * 1.05M + 1.525M", "https://my.mega.be/resources/tarif/Mega-NL-EL-B2C-VL-052025-Dynamic.pdf");
//await flagRepository.SetInjectionTariffExpressionFlag("price * 1.0M - 4.0M", "https://my.mega.be/resources/tarif/Mega-NL-EL-B2C-VL-052025-Dynamic.pdf");

var dbContext = serviceProvider.GetRequiredService<MijnThuisDbContext>();
var historicEnergyEntries = await dbContext.EnergyHistory.Where(x => x.ActiveTarrif == 0).ToListAsync();
var costEntries = await dbContext.DayAheadEnergyPrices.ToListAsync();

foreach (var entry in historicEnergyEntries)
{
    var costEntry = costEntries
        .Where(x => x.From == entry.Date)
        .FirstOrDefault();

    if (costEntry is not null && entry.TotalImportDelta < 1000M)
    {
        entry.CalculatedImportCost = entry.TotalImportDelta * costEntry.ConsumptionCentsPerKWh * 1.06M / 100M;
        entry.CalculatedExportCost = entry.TotalExportDelta * -costEntry.InjectionCentsPerKWh / 100M;
    }
}

await dbContext.SaveChangesAsync();

//dbContext.EnergyInvoices.Add(new EnergyInvoiceEntry
//{
//    Date = new DateTime(2025, 5, 1),
//    ElectricityAmount = 17.43M,
//    GasAmount = 19.16M
//});
//await dbContext.SaveChangesAsync();


//var pieken = ReadPiekenFromCsv();
//var verbruiken = ReadVerbruikenFromCsv();


//var date = new DateTime(2023, 1, 1);
//var to = new DateTime(2025, 2, 11);

//int batchCount = 0;

//while (date < to)
//{
//    var thisMonth = new DateTime(date.Year, date.Month, 1);
//    var monthlyPowerPeak = pieken.ContainsKey(thisMonth) ? pieken[thisMonth] : 0M;

//    //if (!await dbContext.EnergyHistory.AnyAsync(x => x.Date == date))
//    {
//        var verbruik = verbruiken.Where(x => x.DateTime >= date && x.DateTime < date.AddHours(1)).ToList();
//        var tarrif1ImportDelta = verbruik.Where(x => x.Description == "Afname Dag").Select(x => x.Value).Sum();
//        var tarrif2ImportDelta = verbruik.Where(x => x.Description == "Afname Nacht").Select(x => x.Value).Sum();
//        var tarrif1ExportDelta = verbruik.Where(x => x.Description == "Injectie Dag").Select(x => x.Value).Sum();
//        var tarrif2ExportDelta = verbruik.Where(x => x.Description == "Injectie Nacht").Select(x => x.Value).Sum();

//        dbContext.EnergyHistory.Add(new MijnThuis.DataAccess.Entities.EnergyHistoryEntry
//        {
//            Date = date,
//            Tarrif1ImportDelta = tarrif1ImportDelta,
//            Tarrif2ImportDelta = tarrif2ImportDelta,
//            TotalImportDelta = tarrif1ImportDelta + tarrif2ImportDelta,
//            Tarrif1ExportDelta = tarrif1ExportDelta,
//            Tarrif2ExportDelta = tarrif2ExportDelta,
//            TotalExportDelta = tarrif1ExportDelta + tarrif2ExportDelta,
//            MonthlyPowerPeak = monthlyPowerPeak
//        });

//        batchCount++;

//        if (batchCount % (24 * 30) == 0)
//        {
//            Console.WriteLine($"Processed {batchCount} entries, saving to database...");
//            await dbContext.SaveChangesAsync();
//        }
//    }

//    date = date.AddHours(1);
//}

//await dbContext.SaveChangesAsync();

Console.WriteLine("Data has been processed and saved to the database.");

static Dictionary<DateTime, decimal> ReadPiekenFromCsv()
{
    var result = new Dictionary<DateTime, decimal>();
    var lines = File.ReadAllLines("pieken.csv");
    for (int i = 1; i < lines.Length; i++)
    {
        var parts = lines[i].Split(';');
        if (DateTime.TryParse(parts[0], CultureInfo.CurrentCulture, out var date) && decimal.TryParse(parts[8], CultureInfo.CurrentCulture, out var peak))
        {
            result.Add(date, peak);
        }
    }

    return result;
}

static List<CsvEntry> ReadVerbruikenFromCsv()
{
    var lines = File.ReadAllLines("verbruiken.csv");

    var result = new List<CsvEntry>();

    bool isFirstLine = true;
    foreach (var line in lines)
    {
        if (isFirstLine)
        {
            isFirstLine = false;
            continue; // Skip the header line
        }

        var parts = line.Split(';');
        var date = DateTime.Parse(parts[0], CultureInfo.CurrentCulture);
        var time = TimeSpan.Parse(parts[1], CultureInfo.CurrentCulture);

        result.Add(new CsvEntry
        {
            DateTime = date.Add(time),
            Value = string.IsNullOrEmpty(parts[8]) ? 0M : decimal.Parse(parts[8], CultureInfo.CurrentCulture),
            Description = parts[7]
        });
    }

    return result;
}

public class CsvEntry
{
    public DateTime DateTime { get; set; }
    public decimal Value { get; set; }
    public string Description { get; set; }
}