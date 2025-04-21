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
            GameObject UserObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            UnityEngine.Object.Destroy(UserObject.GetComponent<Collider>());
            UnityEngine.Object.DontDestroyOnLoad(UserObject);

            UserObject.name = $"User_{guid.ToString().Substring(0, 8)}";

            users[guid] = new UserData(guid) { UserObject = UserObject };
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

            GameObject userObject = userData.UserObject;
            bool isInSameScene = userData.Scene == SceneManager.GetActiveScene().name;

            if (userObject.activeSelf != isInSameScene)
            {
                userObject.SetActive(isInSameScene);
            }

            if (!isInSameScene)
            {
                return;
            }

            Vector3 old = userData.OldPosition;
            Vector3 next = userData.Position;
            float t = Mathf.Clamp01(userData.TimeSinceData / 0.03f);

            userObject.transform.position = Vector3.Lerp(old, next, t);

            userData.TimeSinceData += Time.fixedUnscaledDeltaTime;
        }

        internal void RemoveUser(Guid guid)
        {
            if (!users.TryRemove(guid, out UserData data))
            {
                return;
            }

            UnityEngine.Object.Destroy(data.UserObject);
        }

        internal void UpdateUserData(Guid guid, Vector3 position)
        {
            if (!users.ContainsKey(guid))
            {
                NewUser(guid);
            }

            users[guid].SetPosition(position);
            users[guid].TimeSinceData = 0f;
        }

        internal void UpdateUserScene(Guid guid, string scene)
        {
            users[guid].Scene = scene;
        }
    }
}
