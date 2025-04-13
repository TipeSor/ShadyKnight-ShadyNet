using System;
using System.IO;
using UnityEngine;

namespace ShadyMP
{
    public class UserData(Guid guid)
    {
        public Guid guid = guid;
        public string name = "name";
        public Vector3 position = Vector3.zero;
        public string scene;
        public GameObject bean;

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
