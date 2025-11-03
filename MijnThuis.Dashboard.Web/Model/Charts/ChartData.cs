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

public class ChartData4<TX, TY> : ChartData3<TX, TY>
{
    public string Series4Description { get; set; }
    public List<ChartDataEntry<TX, TY>> Series4 { get; set; } = new();

    public override void Clear()
    {
        base.Clear();

        Series4.Clear();
    }
}

public class ChartData5<TX, TY> : ChartData4<TX, TY>
{
    public string Series5Description { get; set; }
    public List<ChartDataEntry<TX, TY>> Series5 { get; set; } = new();

    public override void Clear()
    {
        base.Clear();

        Series5.Clear();
    }
}

public class ChartData6<TX, TY> : ChartData5<TX, TY>
{
    public string Series6Description { get; set; }
    public List<ChartDataEntry<TX, TY>> Series6 { get; set; } = new();

    public override void Clear()
    {
        base.Clear();

        Series6.Clear();
    }
}

public class ChartData7<TX, TY> : ChartData6<TX, TY>
{
    public string Series7Description { get; set; }
    public List<ChartDataEntry<TX, TY>> Series7 { get; set; } = new();

    public override void Clear()
    {
        base.Clear();

        Series6.Clear();
    }
}