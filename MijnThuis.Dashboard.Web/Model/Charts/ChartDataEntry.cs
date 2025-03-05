namespace MijnThuis.Dashboard.Web.Model.Charts;

public class ChartDataEntry<TX, TY>
{
    public TX XValue { get; set; }
    public TY YValue { get; set; }

    public override string ToString()
    {
        return $"X: {XValue}, Y: {YValue}";
    }
}