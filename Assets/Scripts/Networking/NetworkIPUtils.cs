using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

namespace Networking
{
    public static class NetworkIPUtils
    {
        /// <summary>
        /// Tenta encontrar o melhor IP da rede local, priorizando Ethernet real.
        /// </summary>
        public static string GetBestLocalIP()
        {
            string wifiFallback = null;
            
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // Ignora interfaces que não estão ativas
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;

                    bool isEthernet = ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet;
                    bool isWifi = ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;

                    if (!isEthernet && !isWifi) continue;

                    // Filtra adaptadores virtuais comuns (VMware, VirtualBox, WSL, vEthernet)
                    string desc = ni.Description.ToLower();
                    string name = ni.Name.ToLower();
                    if (desc.Contains("virtual") || desc.Contains("pseudo") || desc.Contains("vmware") || 
                        desc.Contains("vbox") || name.Contains("virtual") || name.Contains("vethernet") ||
                        name.Contains("wsl"))
                        continue;

                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ipStr = ip.Address.ToString();
                            if (ipStr == "127.0.0.1") continue;

                            // Se for Ethernet, é o padrão ouro na escola, retorna logo.
                            if (isEthernet) 
                            {
                                Debug.Log($"[NetworkIPUtils] IP Ethernet encontrado: {ipStr} ({ni.Name})");
                                return ipStr;
                            }
                            
                            // Se for Wifi, guarda mas continua procurando Ethernet
                            if (isWifi && wifiFallback == null) wifiFallback = ipStr;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkIPUtils] Erro ao listar interfaces: {ex.Message}");
            }

            if (wifiFallback != null) 
            {
                Debug.Log($"[NetworkIPUtils] Usando IP Wi-Fi como fallback: {wifiFallback}");
                return wifiFallback;
            }

            // Fallback final via DNS (pode retornar IPs virtuais, mas é melhor que nada)
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString() != "127.0.0.1")
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }

            return "127.0.0.1";
        }
    }
}