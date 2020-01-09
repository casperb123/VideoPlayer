using System;
using System.Security.Cryptography;

namespace VideoPlayer.Entities
{
    public class Protected
    {
        private readonly byte[] salt;

        public Protected()
        {
            if (string.IsNullOrEmpty(Settings.Default.Salt))
            {
                salt = Guid.NewGuid().ToByteArray();
                Settings.Default.Salt = Convert.ToBase64String(salt);
                Settings.Default.Save();
            }
            else
                salt = Convert.FromBase64String(Settings.Default.Salt);
        }

        protected byte[] Protect(byte[] data)
        {
            try
            {
                return ProtectedData.Protect(data, salt, DataProtectionScope.CurrentUser);
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
                return ProtectedData.Unprotect(data, salt, DataProtectionScope.CurrentUser);
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
