using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MijnThuis.Application.DependencyInjection;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;

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

var flagRepository = serviceProvider.GetRequiredService<IFlagRepository>();
await flagRepository.SetElectricityTariffDetailsFlag(greenEnergyContribution: 1.554M, capacityTariff: 53.2565412M, usageTariff: 5.99007M, dataAdministration: 18.56M, specialExciseTax: 5.03288M, energyContribution: 0.20417M);
//await flagRepository.SetSamsungTheFrameTokenFlag("", new TimeSpan(8, 0, 0), new TimeSpan(22, 0, 0));
//await flagRepository.SetConsumptionTariffExpressionFlag("price * 1.05M + 1.525M", "https://my.mega.be/resources/tarif/Mega-NL-EL-B2C-VL-052025-Dynamic.pdf");
//await flagRepository.SetInjectionTariffExpressionFlag("price * 1.0M - 4.0M", "https://my.mega.be/resources/tarif/Mega-NL-EL-B2C-VL-052025-Dynamic.pdf");

//var dbContext = serviceProvider.GetRequiredService<MijnThuisDbContext>();
//var historicEnergyEntries = await dbContext.EnergyHistory.ToListAsync();

//foreach (var entry in historicEnergyEntries)
//{
//    var costEntry = await dbContext.DayAheadEnergyPrices
//        .Where(x => x.From == entry.Date.AddHours(-1))
//        .FirstOrDefaultAsync();

//    if (costEntry is not null && entry.TotalImportDelta < 1000M)
//    {
//        entry.CalculatedImportCost = entry.TotalImportDelta * costEntry.ConsumptionCentsPerKWh * 1.06M / 100M;
//        entry.CalculatedExportCost = entry.TotalExportDelta * -costEntry.InjectionCentsPerKWh / 100M;
//    }
//}

//dbContext.EnergyInvoices.Add(new EnergyInvoiceEntry
//{
//    Date = new DateTime(2025, 5, 1),
//    ElectricityAmount = 17.43M,
//    GasAmount = 19.16M
//});
//await dbContext.SaveChangesAsync();