using System;
using System.Net;
using System.Net.Sockets;

namespace TMPMS.Utils
{
    // Chặn SSRF khi server tự fetch một URL do client cung cấp (vd. tải ảnh sản phẩm từ URL):
    // không cho phép scheme khác http(s), và không cho phép trỏ tới localhost/IP nội bộ/link-local
    // (bao gồm 169.254.169.254 — endpoint metadata thường gặp trên các nền tảng cloud).
    public static class SsrfGuard
    {
        public static bool IsUnsafeFetchTarget(Uri uri)
        {
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return true;
            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;

            IPAddress[] addresses;
            if (IPAddress.TryParse(uri.Host, out var direct))
            {
                addresses = new[] { direct };
            }
            else
            {
                try { addresses = Dns.GetHostAddresses(uri.Host); }
                catch { return true; }
            }

            foreach (var ip in addresses)
            {
                if (IPAddress.IsLoopback(ip)) return true;

                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var b = ip.GetAddressBytes();
                    if (b[0] == 10) return true;                              // 10.0.0.0/8
                    if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;  // 172.16.0.0/12
                    if (b[0] == 192 && b[1] == 168) return true;               // 192.168.0.0/16
                    if (b[0] == 169 && b[1] == 254) return true;               // 169.254.0.0/16 (link-local, cloud metadata)
                    if (b[0] == 127) return true;                              // 127.0.0.0/8
                    if (b[0] == 0) return true;                                // 0.0.0.0/8
                }
                else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;
                    var b = ip.GetAddressBytes();
                    if (b[0] == 0xfc || b[0] == 0xfd) return true;             // fc00::/7 unique local
                }
            }

            return false;
        }

        public static bool IsUnsafeFetchTarget(string url)
        {
            return !Uri.TryCreate(url, UriKind.Absolute, out var uri) || IsUnsafeFetchTarget(uri);
        }
    }
}
