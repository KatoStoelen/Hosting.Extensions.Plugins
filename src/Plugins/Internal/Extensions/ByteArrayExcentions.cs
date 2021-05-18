namespace Hosting.Extensions.Plugins.Internal.Extensions
{
    internal static class ByteArrayExcentions
    {
        public static bool IsEqualTo(this byte[] first, byte[]? other)
        {
            if (other == null)
            {
                return false;
            }

            if (first.Length != other.Length)
            {
                return false;
            }

            for (var i = 0; i < first.Length; i++)
            {
                if (first[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}