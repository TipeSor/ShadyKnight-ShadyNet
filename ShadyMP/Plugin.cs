using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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
        private string username = "";

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
                        _ = Task.Run(static () => NetworkManager.Instance.Disconnect());
                    }
                }

                GUILayout.Label($"State: {NetworkManager.Instance.state}");
                GUILayout.Label($"IsConnected: {NetworkManager.Instance.IsConnected}");

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
            DrawInputField("Address", ref address, options: option);
            DrawInputField("Port", ref port, options: option);
            DrawInputField("UserName", ref username, 16, option);

            if (GUILayout.Button("Connect") && ushort.TryParse(port, out ushort parsedPort))
            {
                _ = NetworkManager.Instance.ConnectAsync(address, parsedPort, name);
            }
        }

        private void DrawInputField(string label, ref string backing, int MaxLength = int.MaxValue, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:");
            backing = GUILayout.TextField(backing, MaxLength, options);
            GUILayout.EndHorizontal();
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
