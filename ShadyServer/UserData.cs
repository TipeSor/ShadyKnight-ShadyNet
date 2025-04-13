using System;
using System.IO;
using UnityEngine;

namespace ShadyServer
{
    public class UserData(string name)
    {
        public string name = name;
        public Vector3 position = Vector3.zero;
        public Vector3 lastPosition = Vector3.zero;
        public string scene;

        public Guid guid = Guid.NewGuid();
        public StreamWriter writer;

        public void SetPosition(Vector3 newPosition)
        {
            position = newPosition;
        }

        public override string ToString()
        {
            return $"{name} {position.x} {position.y} {position.z}";
        }
    }
}
