using System;
using UnityEngine;

namespace ShadyMP
{
    public class UserData(Guid guid)
    {
        public Guid Guid { get; } = guid;
        public Vector3 Position { get; private set; } = Vector3.zero;
        public Vector3 OldPosition { get; private set; } = Vector3.zero;
        public string Scene { get; set; } = "";
        public GameObject UserObject { get; set; }
        public float TimeSinceData { get; set; } = 0;

        public void SetPosition(Vector3 position)
        {
            OldPosition = Position;
            Position = position;
        }

        public override string ToString()
        {
            return $"{Guid} {Position.x:F2} {Position.y:F2} {Position.z:F2} {Scene}";
        }
    }
}
