namespace EtlViewer.Internal
{
    using System;

    /// <summary>
    /// This class is used to save the cost of Boxing/Unboxing boolean values
    /// </summary>
    internal static class BooleanBoxes
    {
        public static readonly Object TrueBox = true;
        public static readonly Object FalseBox = false;

        public static object Box(Boolean value)
        {
            if (value)
            {
                return TrueBox;
            }
            else
            {
                return FalseBox;
            }
        }
    }
}
