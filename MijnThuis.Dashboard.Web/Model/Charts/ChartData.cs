using MijnThuis.Dashboard.Web.Components.Charts;

namespace MijnThuis.Dashboard.Web.Model.Charts;

public class ChartData1<TX, TY>
{
    public string Description { get; set; }
    public string Series1Description { get; set; }
    public List<ChartDataEntry<TX, TY>> Series1 { get; set; } = new();

    public virtual void Clear()
    {
        Series1.Clear();
    }
}

public class ChartData2<TX, TY> : ChartData1<TX, TY>
{
    public string Series2Description { get; set; }
    public List<ChartDataEntry<TX, TY>> Series2 { get; set; } = new();

    public override void Clear()
    {
        base.Clear();

        Series2.Clear();
    }
}

public class ChartData3<TX, TY> : ChartData2<TX, TY>
{
    public string Series3Description { get; set; }
    public List<ChartDataEntry<TX, TY>> Series3 { get; set; } = new();

    public override void Clear()
    {
        base.Clear();

        Series3.Clear();
    }
}