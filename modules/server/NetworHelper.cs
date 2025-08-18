using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ProjectNuclearWeaponsManagementSystem.Modules.Server
{
    /// <summary>
    /// Các tiện ích liên quan đến Network, ví dụ lấy IP Wi-Fi.
    /// </summary>
    public static class NetworkHelper
    {
        /// <summary>
        /// Lấy địa chỉ IPv4 của Wi-Fi đang hoạt động trên máy.
        /// </summary>
        /// <returns>Chuỗi IP IPv4 của Wi-Fi</returns>
        /// <exception cref="Exception">Nếu không tìm thấy Wi-Fi đang hoạt động</exception>
        public static string GetLocalWiFiIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && ni.OperationalStatus == OperationalStatus.Up) //Wireless80211 như tên, lấy IP từ wifi, dùng eternet thì nhớ đổi
                {
                    var ipProps = ni.GetIPProperties();
                    foreach (UnicastIPAddressInformation ip in ipProps.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }

            throw new Exception("[WARNING] No active Wi-Fi adapter with an IPv4 address found!");
        }
    }
}
