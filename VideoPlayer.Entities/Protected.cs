using DeviceId;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;

namespace VideoPlayer.Entities
{
    [Serializable]
    public class Protected
    {
        protected byte[] Protect(byte[] data)
        {
            try
            {
                string deviceId = new DeviceIdBuilder()
                        .AddSystemUUID()
                        .ToString();

                return ProtectedData.Protect(data, Encoding.ASCII.GetBytes(deviceId), DataProtectionScope.CurrentUser);
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
                string deviceId = new DeviceIdBuilder()
                        .AddSystemUUID()
                        .ToString();

                return ProtectedData.Unprotect(data, Encoding.ASCII.GetBytes(deviceId), DataProtectionScope.CurrentUser);
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
