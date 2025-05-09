using System;
using ShadyShared;
using UnityEngine;

namespace ShadyMP
{
    public class UserData(Guid guid)
    {
        internal readonly UserState state;
        internal readonly Guid guid = guid;
        internal GameObject userObject;
        internal float TimeSinceData;
    }
}

