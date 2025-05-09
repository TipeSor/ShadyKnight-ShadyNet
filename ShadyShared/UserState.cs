namespace ShadyShared
{
    public class UserState
    {
        public Vector3 Position;
        public Vector3 OldPosition = new(0, 0, 0);
        public string SceneName;
        public string UserName = "";

        public UserState(float x, float y, float z, string scene)
        {
            Position = new Vector3(x, y, z);
            SceneName = scene;
        }

        public UserState(Vector3 position, string scene)
        {
            Position = position;
            SceneName = scene;
        }

        public UserState()
        {
            Position = new Vector3(0, 0, 0);
            SceneName = "Temp";
        }

        public byte[] Serialize()
        {
            byte[] _position = BitGood.GetBytes(Position);         // 12 bytes
            byte[] _scene = BitGood.GetBytes(SceneName);           // N bytes
            byte[] _scenebuffer = BitGood.GetBytes(_scene.Length); // 4 bytes
            byte[] _name = BitGood.GetBytes(UserName);             // N bytes
            byte[] _namebuffer = BitGood.GetBytes(_name.Length);   // 4 bytes 

            byte[] data = Utils.Combine(_position, _scenebuffer, _scene, _namebuffer, _name);

            return data;
        }

        public void Deserialize(byte[] value, int startIndex)
        {
            OldPosition = Position;
            Position = BitGood.ToVector3(value, startIndex);
            startIndex += 12;

            int SceneBuffer = BitGood.ToInt(value, startIndex);
            startIndex += 4;

            SceneName = BitGood.ToString(value, startIndex, SceneBuffer);
            startIndex += SceneBuffer;

            int NameBuffer = BitGood.ToInt(value, startIndex);
            startIndex += 4;

            UserName = BitGood.ToString(value, startIndex, NameBuffer);
        }
    }
}
