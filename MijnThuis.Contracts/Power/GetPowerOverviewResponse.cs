﻿namespace MijnThuis.Contracts.Power;

public record GetPowerOverviewResponse
{
    public decimal CurrentPower { get; init; }
    public decimal CurrentConsumption { get; set; }
    public int PowerPeak { get; init; }
    public decimal EnergyToday { get; set; }
    public decimal EnergyThisMonth { get; set; }
    public string CurrentPricePeriod { get; set; }
    public decimal CurrentConsumptionPrice { get; set; }
    public decimal CurrentInjectionPrice { get; set; }
    public bool IsTvOn { get; set; }
    public bool IsBureauOn { get; set; }
    public bool IsVijverOn { get; set; }
}