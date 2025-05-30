using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShadyShared
{
    public class Writer
    {
        public ConcurrentQueue<WriteContext> WriteQueue { get; } = new ConcurrentQueue<WriteContext>();
        private readonly CancellationTokenSource source = new();
        private readonly Task writingLoop;
        private bool disposed;

        public Writer()
        {
            writingLoop = Task.Run(() => RunAsync(source.Token));
        }

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!WriteQueue.TryDequeue(out WriteContext? context))
                {
                    await Task.Delay(10, token).ConfigureAwait(false);
                    continue;
                }

                foreach (NetworkStream stream in context.Reciever)
                {
                    try
                    {
                        await Protocol.WritePacketAsync(stream, context.Data, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Dispatcher] Write failed: {ex.Message}");
                    }
                }

                if (context.Dispose)
                {
                    context.Sender.Close();
                }
            }
        }

        public void Enqueue(WriteContext context)
        {
            WriteQueue.Enqueue(context);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            source.Cancel();
            writingLoop.Wait();
            source.Dispose();
        }
    }

    public class WriteContext(TcpClient sender, NetworkStream[] receivers, byte[] data)
    {
        public TcpClient Sender { get; } = sender;
        public NetworkStream[] Reciever { get; } = receivers;
        public byte[] Data { get; } = data;
        public bool Dispose { get; set; } = false;
    }
}
