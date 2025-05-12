using System;
using System.Collections.Concurrent;
using BepInEx;
using BepInEx.Logging;
using ShadyShared;
using UnityEngine;

#pragma warning disable IDE0051
namespace ShadyMP
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    internal class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;


        private string address = "";
        private string port = "";

        internal static readonly ConcurrentQueue<Action> MainThreadQueue = new();

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");
            ProtocolHandler.RegisterHandlers();
        }

        internal void OnGUI()
        {
            try
            {
                GUILayoutOption option = GUILayout.Width(120f);

                if (!NetworkManager.Instance.IsConnected)
                {
                    DrawConnectionUI(option);
                }
                else
                {
                    if (GUILayout.Button("Leave"))
                    {
                        NetworkManager.Instance.Disconnect();
                    }
                }

                if (Game.debug && Game.player != null)
                {
                    GUILayout.Label($"Position: {Game.player.t.position}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void DrawConnectionUI(GUILayoutOption option)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("IP Address:");
            address = GUILayout.TextField(address, option);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Port:");
            port = GUILayout.TextField(port, option);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Connect") && int.TryParse(port, out int parsedPort))
            {
                _ = NetworkManager.Instance.ConnectAsync(address, parsedPort);
            }
        }

        private void FixedUpdate()
        {
            try
            {
                while (MainThreadQueue.TryDequeue(out Action action))
                {
                    action();
                }

                UserManager.Instance.UpdateUsers();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
