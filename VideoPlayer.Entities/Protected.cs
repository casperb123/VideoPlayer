using System;
using System.Security.Cryptography;

namespace VideoPlayer.Entities
{
    public class Protected
    {
        private readonly byte[] salt;

        public Protected()
        {
            if (GlobalSettings.Settings is null)
                return;

            if (GlobalSettings.Settings.Salt is null || GlobalSettings.Settings.Salt.Length <= 0)
            {
                salt = Guid.NewGuid().ToByteArray();
                GlobalSettings.Settings.Salt = salt;
                GlobalSettings.Settings.Save().ConfigureAwait(false);
            }
            else
                salt = GlobalSettings.Settings.Salt;
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
