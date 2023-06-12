using System.Buffers;
using System.Text;
using Xunit.Abstractions;

namespace KdSoft.NamedMessagePipe.Tests
{
    public class NamedPipeTests
    {
        readonly ITestOutputHelper _output;
        public const string PipeName = "elekta-pipe-test";

        public NamedPipeTests(ITestOutputHelper output) {
            this._output = output;
        }

        string GetString(ReadOnlySequence<byte> sequence) {
#if NETCOREAPP3_1_OR_GREATER
            return Encoding.UTF8.GetString(sequence.ToArray());
#else
            return Encoding.UTF8.GetString(msgSequence);
#endif
        }

        [Fact]
        public async Task ClientSendMessage() {
            using var cts = new CancellationTokenSource();
            using var server = new NamedMessagePipeServer(PipeName, cts.Token, 16);
            var readTask = Task.Run(async () => {
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                }
                _output.WriteLine("End of messages");
            });

            using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName).ConfigureAwait(false);
            for (int indx = 0; indx < 10; indx++) {
                await client.WriteAsync(Encoding.UTF8.GetBytes($"A long message exceeding 16 bytes, index: {indx}")).ConfigureAwait(false);
            }
            client.WaitForPipeDrain();

            // brief delay to let _output catch up
            cts.CancelAfter(100);
            await readTask.ConfigureAwait(false);
        }

        [Fact]
        public async Task MultipleClientSendMessage() {
            using var cts = new CancellationTokenSource();

            using var server1 = new NamedMessagePipeServer(PipeName, cts.Token, 16);
            var server1Task = Task.Run(async () => {
                await foreach (var msgSequence in server1.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                }
                _output.WriteLine("Server1: End of messages");
            });

            using var server2 = new NamedMessagePipeServer(PipeName, cts.Token, 16);
            var server2Task = Task.Run(async () => {
                await foreach (var msgSequence in server2.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                }
                _output.WriteLine("Server2: End of messages");
            });

            async Task RunClient(int clientIndex) {
                using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName).ConfigureAwait(false);
                for (int indx = 0; indx < 10; indx++) {
                    await client.WriteAsync(Encoding.UTF8.GetBytes($"Client{clientIndex}: a message exceeding 16 bytes, index: {indx}")).ConfigureAwait(false);
                }
                client.WaitForPipeDrain();
            }

            var client1Task = RunClient(1);
            var client2Task = RunClient(2);
            var client3Task = RunClient(3);
            var client4Task = RunClient(4);
            await Task.WhenAll(client1Task, client2Task, client3Task, client4Task).ConfigureAwait(false);

            // brief delay to let _output catch up
            cts.CancelAfter(100);
            await server1Task.ConfigureAwait(false);
            await server2Task.ConfigureAwait(false);
        }

        [Fact]
        public async Task ServerSendMessage() {
            using var serverCts = new CancellationTokenSource();
            using var server = new NamedMessagePipeServer(PipeName, serverCts.Token, 16);

            // server listens for incoming messages and replies with a number of messages
            var serverTask = Task.Run(async () => {
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    for (int indx = 0; indx < 10; indx++) {
                        await server.WriteAsync(Encoding.UTF8.GetBytes($"A long message exceeding 16 bytes, index: {indx}")).ConfigureAwait(false);
                    }
                    await server.WriteAsync(Encoding.UTF8.GetBytes("Last Message")).ConfigureAwait(false);
                }
                _output.WriteLine("End of messages");
            });

            using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName).ConfigureAwait(false);
            await client.WriteAsync(Encoding.UTF8.GetBytes($"Hello server")).ConfigureAwait(false);
            client.WaitForPipeDrain();

            // the client's listening loop will only end when the client is disposed, as this triggers the read/listen cancellation token;
            // it depends on the server sending the termination message
            var clientCts = new CancellationTokenSource();
            await foreach (var msgSequence in client.Messages(clientCts.Token).ConfigureAwait(false)) {
                var msg = GetString(msgSequence);
                _output.WriteLine(msg);
                if (msg == "Last Message") {
                    clientCts.Cancel();
                }
            }

            try {
                // brief delay to let _output catch up
                serverCts.CancelAfter(100);
                await serverTask.ConfigureAwait(false);
            }
            catch (Exception ex) {
                _output.WriteLine(ex.ToString());
            }
        }

        [Fact]
        public async Task SendReplyMessage() {
            using var serverCts = new CancellationTokenSource();
            using var server = new NamedMessagePipeServer(PipeName, serverCts.Token, 16);

            var serverTask = Task.Run(async () => {
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    var reply = Encoding.UTF8.GetBytes($"Reply to {msg}");
                    await server.WriteAsync(reply).ConfigureAwait(false);
                }
                _output.WriteLine("Server: End of messages");
            });

            using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName).ConfigureAwait(false);
            for (int indx = 0; indx < 10; indx++) {
                await client.WriteAsync(Encoding.UTF8.GetBytes($"A very long message exceeding 16 bytes, index: {indx}")).ConfigureAwait(false);

                // we can only end the listening loop by cancellation, otherwise the loop will hang
                var clientCts = new CancellationTokenSource();

                //var iterator = client.Messages(clientCts.Token).GetAsyncEnumerator();
                //while (await iterator.MoveNextAsync().ConfigureAwait(false)) {
                //    var msgSequence = iterator.Current;
                //    var msg = Encoding.UTF8.GetString(msgSequence);
                //    _output.WriteLine(msg);
                //    clientCts.Cancel();
                //}
                //await iterator.DisposeAsync().ConfigureAwait(false);

                // this restarts the listener
                await foreach (var msgSequence in client.Messages(clientCts.Token).ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    // we expect only one message, so we end the loop
                    clientCts.Cancel();
                }

                client.WaitForPipeDrain();

                // If we want to restart listening and reading we need to reset the pipeline!
                // Note: both the pipeline reader and writer must have commpleted!
                client.Reset();
            }

            // brief delay to let _output catch up
            serverCts.CancelAfter(100);
            await serverTask.ConfigureAwait(false);
        }

        [Fact]
        public async Task MultipleClientSendReplyMessage() {
            using var serverCts = new CancellationTokenSource();

            using var server1 = new NamedMessagePipeServer(PipeName, serverCts.Token, 16);
            var server1Task = Task.Run(async () => {
                await foreach (var msgSequence in server1.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    var reply = Encoding.UTF8.GetBytes($"Server1 reply to {msg}");
                    await server1.WriteAsync(reply).ConfigureAwait(false);
                }
                _output.WriteLine("Server1: End of messages");
            });

            using var server2 = new NamedMessagePipeServer(PipeName, serverCts.Token, 16);
            var server2Task = Task.Run(async () => {
                await foreach (var msgSequence in server2.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    var reply = Encoding.UTF8.GetBytes($"Server2 reply to {msg}");
                    await server2.WriteAsync(reply).ConfigureAwait(false);
                }
                _output.WriteLine("Server2: End of messages");
            });

            async Task RunClient(int clientIndex) {
                using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName).ConfigureAwait(false);
                for (int indx = 0; indx < 10; indx++) {
                    await client.WriteAsync(Encoding.UTF8.GetBytes($"Client{clientIndex}: a message exceeding 16 bytes, index: {indx}")).ConfigureAwait(false);

                    // we can only end the listening loop by cancellation, otherwise the loop will hang
                    var clientCts = new CancellationTokenSource();

                    // this restarts the listener
                    await foreach (var msgSequence in client.Messages(clientCts.Token).ConfigureAwait(false)) {
                        var msg = GetString(msgSequence);
                        _output.WriteLine(msg);
                        // we expect only one message, so we end the loop
                        clientCts.Cancel();
                    }

                    // If we want to restart listening and reading we need to reset the pipeline!
                    // Note: both the pipeline reader and writer must have commpleted!
                    client.Reset();
                }
                client.WaitForPipeDrain();
            }

            var client1Task = RunClient(1);
            var client2Task = RunClient(2);
            var client3Task = RunClient(3);
            var client4Task = RunClient(4);
            await Task.WhenAll(client1Task, client2Task, client3Task, client4Task).ConfigureAwait(false);

            // brief delay to let _output catch up
            serverCts.CancelAfter(100);
            await server1Task.ConfigureAwait(false);
            await server2Task.ConfigureAwait(false);
        }
    }
}
