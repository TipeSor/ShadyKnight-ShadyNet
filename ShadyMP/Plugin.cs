using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShadyMP
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        internal TcpClient me;
        internal NetworkStream stream;

        string address = "";
        string port = "";

        ConcurrentDictionary<Guid, UserData> users = [];
        ConcurrentQueue<Action> beans = new();

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");
        }


        public void OnGUI()
        {
            if (Game.debug && Game.player != null)
            {
                GUILayout.Label($"Position: {Game.player.t.position}");
            }

            if (me == null || !me.Connected)
            {
                GUILayoutOption option = GUILayout.Width(120f);
                GUILayout.BeginHorizontal();
                GUILayout.Label("IPAdress:");
                address = GUILayout.TextField(address, option);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Port:");
                port = GUILayout.TextField(port, option);
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Connect"))
                {
                    if (int.TryParse(port, out int parsedPort))
                    {
                        Connect(address, parsedPort);
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Leave"))
                {
                    me.Dispose();
                }
            }
        }

        private void FixedUpdate()
        {
            while (beans.TryDequeue(out Action action))
            {
                action();
            }

            foreach (Guid guid in users.Keys)
            {
                UpdateBean(guid);
            }
        }

        public void Connect(string address, int port)
        {
            try
            {
                me ??= new TcpClient();
                me.Connect(address, port);
                stream = me.GetStream();

                _ = Task.Run(() => HandleServer());
                _ = Task.Run(() => SendData());
            }
            catch (Exception)
            {
                return;
            }
        }

        public async Task HandleServer()
        {
            using StreamReader reader = new(stream, Encoding.UTF8);

            try
            {
                while (me.Connected)
                {
                    string data = await reader.ReadLineAsync();
                    if (data == null)
                    {
                        break;
                    }

                    string[] splitData = data.Split(' ');
                    string command = splitData[0];
                    string[] args = [.. splitData.Skip(1)];

                    switch (command)
                    {
                        case "update-data":
                            Guid guid = Guid.Parse(args[0]);
                            if (!users.ContainsKey(guid))
                            {
                                users[guid] = new UserData(guid);
                                beans.Enqueue(() => CreateBean(guid));
                            }
                            float x = float.Parse(args[1]);
                            float y = float.Parse(args[2]);
                            float z = float.Parse(args[3]);
                            users[guid].scene = args[4];

                            users[guid].SetPosition(new Vector3(x, y, z));
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Disconnect();
            }
        }


        public async Task SendData()
        {
            using StreamWriter writer = new(stream, Encoding.UTF8);
            while (me.Connected)
            {
                if (Game.player == null)
                {
                    await Task.Delay(30);
                    continue;
                }
                Vector3 position = Game.player.t.position;
                string sceneName = SceneManager.GetActiveScene().name;
                string data = $"setposition {position.x} {position.y} {position.z} {sceneName}";
                await writer.WriteLineAsync(data);
                await writer.FlushAsync();
                await Task.Delay(30);
            }
        }

        public void Disconnect()
        {
            me.Dispose();
            me = null;
            stream = null;
        }

        public void CreateBean(Guid guid)
        {
            GameObject bean = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(bean.GetComponent<Collider>());
            DontDestroyOnLoad(bean);

            bean.name = guid.ToString();

            bean.transform.position = users[guid].position;
            users[guid].bean = bean;
        }

        public void UpdateBean(Guid guid)
        {
            GameObject bean = users[guid].bean;

            bool sameScene = users[guid].scene == SceneManager.GetActiveScene().name;

            if (bean.activeSelf != sameScene)
            {
                bean.SetActive(sameScene);
            }

            if (!sameScene)
            {
                return;
            }

            Vector3 startPosition = users[guid].bean.transform.position;
            Vector3 endPosition = users[guid].position;

            float t = 0.5555f;
            users[guid].bean.transform.position = Vector3.Lerp(startPosition, endPosition, t);
        }
    }
}
