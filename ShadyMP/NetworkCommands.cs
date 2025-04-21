using System;
using UnityEngine;
#pragma warning disable IDE1006
namespace ShadyMP
{
    public class NetworkCommands
    {
        [Command]
        internal static void updatedata(Guid guid, float x, float y, float z)
        {
            Vector3 position = new(x, y, z);
            UserManager.Instance.UpdateUserData(guid, position);
        }

        [Command]
        internal static void updatedscene(Guid guid, string scene)
        {
            UserManager.Instance.UpdateUserScene(guid, scene);
        }
    }
}
