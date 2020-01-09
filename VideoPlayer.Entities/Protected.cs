using System;
using System.Security.Cryptography;
using System.Text;

namespace VideoPlayer.Entities
{
    public class Protected
    {
        protected byte[] Protect(byte[] data)
        {
            try
            {
                if (GlobalSettings.Settings is null)
                    return ProtectedData.Protect(data, Encoding.ASCII.GetBytes(Environment.UserName), DataProtectionScope.CurrentUser);
                else
                    return ProtectedData.Protect(data, GlobalSettings.Settings.Salt, DataProtectionScope.CurrentUser);
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
                return ProtectedData.Unprotect(data, GlobalSettings.Settings.Salt, DataProtectionScope.CurrentUser);
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
