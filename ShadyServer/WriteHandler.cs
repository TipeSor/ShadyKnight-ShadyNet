using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using ShadyShared;
#pragma warning disable CA1720
namespace ShadyServer
{
    public static class WriteHandler
    {
        public static readonly Channel<WriteContext> WriteQueue = Channel.CreateBounded<WriteContext>(1000);

        public static void EnqueueWrite(WriteContext context)
        {
            _ = Task.Run(() => EnqueueWriteAsync(context));
        }

        public static async Task EnqueueWriteAsync(WriteContext context)
        {
            await WriteQueue.Writer.WriteAsync(context);
        }

        public static async Task WriteLoop()
        {
            while (Program.IsRunning)
            {
                _ = await WriteQueue.Reader.WaitToReadAsync();
                while (WriteQueue.Reader.TryRead(out WriteContext context))
                {
                    switch (context.Type)
                    {
                        case WriteType.Single:
                            await Single(context);
                            break;
                        case WriteType.Broadcast:
                            await Broadcast(context);
                            break;
                        case WriteType.Share:
                            await Share(context);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public static async Task Single(WriteContext context)
        {
            await Protocol.WritePacketAsync(context.Stream, context.Data);
        }

        public static async Task Broadcast(WriteContext context)
        {
            foreach (UserData clientData in UserHandler.Users.Values)
            {
                await Protocol.WritePacketAsync(clientData.Stream, context.Data);
            }
        }

        public static async Task Share(WriteContext context)
        {
            foreach ((TcpClient user, UserData userData) in UserHandler.Users)
            {
                if (user == context.Sender)
                {
                    continue;
                }

                await Protocol.WritePacketAsync(userData.Stream, context.Data);
            }
        }
    }

    public enum WriteType
    {
        Single,
        Broadcast,
        Share
    }

    public class WriteContext(WriteType type, TcpClient sender, byte[] data)
    {
        public WriteType Type { get; } = type;
        public TcpClient Sender { get; } = sender;
        public NetworkStream Stream { get; } = sender.GetStream();
        public byte[] Data { get; } = data;
    }
}
