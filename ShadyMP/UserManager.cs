using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShadyMP
{
    internal class UserManager
    {
        internal static UserManager Instance { get; } = new();

        private readonly ConcurrentDictionary<Guid, UserData> users = [];

        internal void NewUser(Guid guid)
        {
            Plugin.MainThreadQueue.Enqueue(() =>
            {
                GameObject UserObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                UnityEngine.Object.Destroy(UserObject.GetComponent<Collider>());
                UnityEngine.Object.DontDestroyOnLoad(UserObject);

                UserObject.name = $"User_{guid.ToString().Substring(0, 8)}";

                users[guid] = new UserData(guid) { userObject = UserObject };
                Plugin.Logger.LogInfo($"created user `{UserObject.name}`");
            });
        }

        internal bool TryGetUser(Guid guid, out UserData data)
        {
            return users.TryGetValue(guid, out data);
        }

        internal void RemoveUser(Guid guid)
        {
            UserData data;
            while (!users.TryRemove(guid, out data))
            {
                continue;
            }
            Plugin.Logger.LogInfo($"enqueuing {data.guid} object to be destroyed");
            Plugin.MainThreadQueue.Enqueue(() =>
            {
                UnityEngine.Object.Destroy(data.userObject);
                Plugin.Logger.LogInfo($"destrotyed {data.guid} object");
            });
        }

        internal void RemoveUsers()
        {
            foreach (Guid guid in users.Keys)
            {
                RemoveUser(guid);
            }
        }

        internal void UpdateUsers()
        {
            foreach (Guid guid in users.Keys)
            {
                UpdateUser(guid);
            }
        }

        internal void UpdateUser(Guid guid)
        {
            UserData userData = users[guid];

            GameObject userObject = userData.userObject;
            bool isInSameScene = userData.state.SceneName == SceneManager.GetActiveScene().name;

            if (userObject.activeSelf != isInSameScene)
            {
                userObject.SetActive(isInSameScene);
            }

            if (!isInSameScene)
            {
                return;
            }

            Vector3 old = userData.state.OldPosition;
            Vector3 next = userData.state.Position;
            float t = Mathf.Clamp01(userData.TimeSinceData);

            userObject.transform.position = Vector3.Lerp(old, next, t);

            userData.TimeSinceData += Time.fixedUnscaledDeltaTime / 0.01f;
        }
    }
}
