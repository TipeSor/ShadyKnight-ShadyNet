using System;
using System.IO;
using UnityEngine;

namespace ShadyServer
{
    public class UserData(string name)
    {
        public string Name { get; set; } = name;
        public Vector3 Position { get; set; } = Vector3.zero;
        public Vector3 LastPosition { get; set; } = Vector3.zero;
        public string Scene { get; set; }
        private readonly Guid guid = Guid.NewGuid();

        public Guid GetGuid()
        {
            return guid;
        }

        public StreamWriter Writer { get; set; }


        public void SetPosition(Vector3 newPosition)
        {
            Position = newPosition;
        }

        public override string ToString()
        {
            return $"{GetGuid()} {Position.x:F2} {Position.y:F2} {Position.z:F2}";
        }
    }
}
