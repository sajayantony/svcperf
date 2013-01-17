namespace EtlViewerQuery
{
    /// <summary>
    /// Used by the query framework and is a publicly accessible
    /// </summary>
    public class DurationItem
    {
        public double Duration { get; set; }
        public long Count { get; set; }

        public double ValueX { get { return Duration; } }
        public double ValueY { get { return Count; } }
    }
}
