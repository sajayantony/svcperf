namespace Viewer.Utilities
{
    using System;

    public static class Extensions
    {
        public static Guid ToGuid(this string t)
        {
            return new Guid(t);
        }
    }
}
