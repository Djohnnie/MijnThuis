using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MijnThuis.Application.DependencyInjection;
using MijnThuis.DataAccess;
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

//var flagRepository = serviceProvider.GetRequiredService<IFlagRepository>();
//await flagRepository.SetConsumptionTariffExpressionFlag("price * 1.05M + 1.525M", "https://my.mega.be/resources/tarif/Mega-NL-EL-B2C-VL-052025-Dynamic.pdf");
//await flagRepository.SetInjectionTariffExpressionFlag("price * 1.0M - 4.0M", "https://my.mega.be/resources/tarif/Mega-NL-EL-B2C-VL-052025-Dynamic.pdf");

var dbContext = serviceProvider.GetRequiredService<MijnThuisDbContext>();
var historicEnergyEntries = await dbContext.EnergyHistory.ToListAsync();

foreach (var entry in historicEnergyEntries)
{
    var costEntry = await dbContext.DayAheadEnergyPrices
        .Where(x => x.From == entry.Date.AddHours(-1))
        .FirstOrDefaultAsync();

    if (costEntry is not null && entry.TotalImportDelta < 1000M)
    {
        entry.CalculatedImportCost = entry.TotalImportDelta * costEntry.ConsumptionCentsPerKWh * 1.06M / 100M;
        entry.CalculatedExportCost = entry.TotalExportDelta * -costEntry.InjectionCentsPerKWh / 100M;
    }
}

await dbContext.SaveChangesAsync();