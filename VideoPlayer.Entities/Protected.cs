using System.Security.Cryptography;

namespace VideoPlayer.Entities
{
    public class Protected
    {
        protected byte[] Protect(byte[] data)
        {
            try
            {
                return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
#if DEBUG
                throw;
#else
                return null;
#endif
            }
        }

        protected byte[] Unprotect(byte[] data)
        {
            try
            {
                return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
#if DEBUG
                throw;
#else
                return null;
#endif
            }
        }
    }
}
