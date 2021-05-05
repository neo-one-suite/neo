namespace ClassLibrary
{
    public record Class(string Str)
    {
        internal int Int
        {
            get; init;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {

    }
}
