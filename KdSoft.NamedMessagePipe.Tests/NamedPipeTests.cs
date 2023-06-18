using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace KdSoft.NamedMessagePipe.Tests
{
#if NETFRAMEWORK
    public class NamedPipeTests: IClassFixture<NamedPipeTestFixtureFramework>
#else
    public class NamedPipeTests: IClassFixture<NamedPipeTestFixtureCore>
#endif
    {
        readonly ITestOutputHelper _output;

        public const string PipeName = "elekta-pipe-test";
        const TaskCreationOptions ServerTaskOptions = TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously;

#if NETFRAMEWORK
        readonly NamedPipeTestFixtureFramework _fixture;
        public NamedPipeTests(ITestOutputHelper output, NamedPipeTestFixtureFramework fixture) {
            this._output = output;
            this._fixture = fixture;
            // kick start the EventPipe session
        }
#else
        readonly NamedPipeTestFixtureCore _fixture;
        public NamedPipeTests(ITestOutputHelper output, NamedPipeTestFixtureCore fixture) {
            this._output = output;
            this._fixture = fixture;
            // kick start the EventPipe session
        }
#endif

        string GetString(ReadOnlySequence<byte> sequence) {
#if NETCOREAPP3_1_OR_GREATER || NETFRAMEWORK
            return Encoding.UTF8.GetString(sequence.ToArray());
#else
            return Encoding.UTF8.GetString(msgSequence);
#endif
        }

        [Fact]
        public async Task ClientSendMessages() {
            NamedPipeEventSource.Log.Write(nameof(ClientSendMessages));

            using var cts = new CancellationTokenSource();

            async Task RunServer() {
                using var server = new NamedMessagePipeServer(PipeName, nameof(ClientSendMessages) + "-Server", cts.Token, 16);
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                }
                _output.WriteLine("End of messages");
            }
            var serverTask = RunServer();

            using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(ClientSendMessages) + "-Client", 500).ConfigureAwait(false);
            for (int indx = 0; indx < 10; indx++) {
                var msgBytes = Encoding.UTF8.GetBytes($"A long message exceeding 16 bytes, index: {indx}");
                await client.WriteAsync(msgBytes, 0, msgBytes.Length).ConfigureAwait(false);
                client.WaitForPipeDrain();
            }

            // brief delay to let _output catch up
            cts.CancelAfter(200);
            await serverTask.ConfigureAwait(false);

            GC.Collect();
        }

        [Fact]
        public async Task MultipleClientSendMessages() {
            using var cts = new CancellationTokenSource();

            async Task RunServer(int serverIndex) {
                using var server = new NamedMessagePipeServer(PipeName, $"{nameof(MultipleClientSendMessages)}-Server{serverIndex}", cts.Token, 16);
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                }
                _output.WriteLine($"Server{serverIndex}: End of messages");
            }

            async Task RunClient(int clientIndex) {
                using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(MultipleClientSendMessages) + "-Client" + clientIndex, 500).ConfigureAwait(false);
                for (int indx = 0; indx < 10; indx++) {
                    var msgBytes = Encoding.UTF8.GetBytes($"Client{clientIndex}: a message exceeding 16 bytes, index: {indx}");
                    await client.WriteAsync(msgBytes, 0, msgBytes.Length).ConfigureAwait(false);
                }
                client.WaitForPipeDrain();
            }

            var server1Task = RunServer(1);
            var server2Task = RunServer(2);

            var client1Task = RunClient(1);
            var client2Task = RunClient(2);
            var client3Task = RunClient(3);
            var client4Task = RunClient(4);
            await Task.WhenAll(client1Task, client2Task, client3Task, client4Task).ConfigureAwait(false);

            // brief delay to let _output catch up
            cts.CancelAfter(200);
            await server1Task.ConfigureAwait(false);
            await server2Task.ConfigureAwait(false);

            GC.Collect();
        }

        [Fact]
        public async Task ServerSendMessages() {
            using var serverCts = new CancellationTokenSource();

            // server listens for incoming messages and replies with a number of messages
            async Task RunServer() {
                using var server = new NamedMessagePipeServer(PipeName, nameof(ServerSendMessages) + "-Server", serverCts.Token, 16);
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    for (int indx = 0; indx < 10; indx++) {
                        var msgBytes = Encoding.UTF8.GetBytes($"A long message exceeding 16 bytes, index: {indx}");
                        await server.WriteAsync(msgBytes, 0, msgBytes.Length).ConfigureAwait(false);
                    }
                    var lastBytes = Encoding.UTF8.GetBytes("Last Message");
                    await server.WriteAsync(lastBytes, 0, lastBytes.Length).ConfigureAwait(false);
                    await server.FlushAsync().ConfigureAwait(false);
                }
                _output.WriteLine("End of messages");
            }
            var serverTask = RunServer();

            using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(ServerSendMessages) + "-Client", 500).ConfigureAwait(false);
            var helloBytes = Encoding.UTF8.GetBytes("Last Message");
            await client.WriteAsync(helloBytes, 0, helloBytes.Length).ConfigureAwait(false);
            client.WaitForPipeDrain();

            // the client's listening loop will only end when the client is disposed, as this triggers the read/listen cancellation token;
            // it depends on the server sending the termination message
            await foreach (var msgSequence in client.Messages().ConfigureAwait(false)) {
                var msg = GetString(msgSequence);
                _output.WriteLine(msg);
                if (msg == "Last Message") {
                    break;
                }
            }

            // brief delay to let _output catch up
            serverCts.CancelAfter(200);
            await serverTask.ConfigureAwait(false);
        }

        [Fact]
        public async Task SendReplyMessage() {
            using var serverCts = new CancellationTokenSource();

            async Task RunServer() {
                using var server = new NamedMessagePipeServer(PipeName, nameof(SendReplyMessage) + "-Server", serverCts.Token, 16);
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    var reply = Encoding.UTF8.GetBytes($"Reply to {msg}");
                    await server.WriteAsync(reply, 0, reply.Length).ConfigureAwait(false);
                    await server.FlushAsync().ConfigureAwait(false);
                }
                _output.WriteLine("Server: End of messages");
            }
            var serverTask = RunServer();

#if !NETFRAMEWORK
            using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(SendReplyMessage) + "-Client", 500).ConfigureAwait(false);
#endif
            var buffer = new byte[1024];
            for (int indx = 0; indx < 10; indx++) {
#if NETFRAMEWORK
                // in full framework we can only use Dispose() to stop the client from reading, cancellation does not work
                using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(SendReplyMessage) + "-Client", 500).ConfigureAwait(false);
#endif
                var msgBytes = Encoding.UTF8.GetBytes($"A very long message exceeding 16 bytes, index: {indx}");
                await client.WriteAsync(msgBytes, 0, msgBytes.Length).ConfigureAwait(false);
                client.WaitForPipeDrain();

                //var iterator = client.Messages().GetAsyncEnumerator();
                //while (await iterator.MoveNextAsync().ConfigureAwait(false)) {
                //    var msgSequence = iterator.Current;
                //    var msg = GetString(msgSequence);
                //    _output.WriteLine(msg);
                //    // we expect only one message, so we end the loop
                //    break;
                //}
                //await iterator.DisposeAsync().ConfigureAwait(false);

                // this restarts the listener
                await foreach (var msgSequence in client.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    // we expect only one message, so we end the loop
                    break;
                }

#if !NETFRAMEWORK
                // If we want to restart listening and reading we need to reset the pipeline!
                // Note: both the pipeline reader and writer must have commpleted!
                client.Reset();
#endif
            }

            // brief delay to let _output catch up
            serverCts.CancelAfter(200);
            await serverTask.ConfigureAwait(false);
        }

        [Fact]
        public async Task MultipleClientSendReplyMessage() {
            using var serverCts = new CancellationTokenSource();

            var server1Task = Task.Factory.StartNew(async () => {
                using var server1 = new NamedMessagePipeServer(PipeName, nameof(MultipleClientSendReplyMessage) + "-Server1", serverCts.Token, 16);
                await foreach (var msgSequence in server1.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    var reply = Encoding.UTF8.GetBytes($"Server1 reply to {msg}");
                    await server1.WriteAsync(reply, 0, reply.Length).ConfigureAwait(false);
                }
                _output.WriteLine("Server1: End of messages");
            }, ServerTaskOptions);

            var server2Task = Task.Factory.StartNew(async () => {
                using var server2 = new NamedMessagePipeServer(PipeName, nameof(MultipleClientSendReplyMessage) + "-Server2", serverCts.Token, 16);
                await foreach (var msgSequence in server2.Messages().ConfigureAwait(false)) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    var reply = Encoding.UTF8.GetBytes($"Server2 reply to {msg}");
                    await server2.WriteAsync(reply, 0, reply.Length).ConfigureAwait(false);
                }
                _output.WriteLine("Server2: End of messages");
            }, ServerTaskOptions);

#if NETFRAMEWORK
            async Task RunClient(int clientIndex, int loopCount) {
                for (int indx = 0; indx < loopCount; indx++) {
                    using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(MultipleClientSendReplyMessage) + "-Client" + clientIndex, 500).ConfigureAwait(false);
                    var msgBytes = Encoding.UTF8.GetBytes($"Client{clientIndex}: a message exceeding 16 bytes, index: {indx}");
                    await client.WriteAsync(msgBytes, 0, msgBytes.Length).ConfigureAwait(false);
                    client.WaitForPipeDrain();

                    // this restarts the listener
                    await foreach (var msgSequence in client.Messages().ConfigureAwait(false)) {
                        var msg = GetString(msgSequence);
                        _output.WriteLine(msg);
                        // we expect only one message, so we end the loop
                        break;
                    }

                    // If we want to restart listening and reading we need to reset the pipeline!
                    // Note: both the pipeline reader and writer must have commpleted!
                }
            }
#else
            async Task RunClient(int clientIndex, int loopCount) {
                using (var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(MultipleClientSendReplyMessage) + "-Client" + clientIndex, 500).ConfigureAwait(false)) {
                    for (int indx = 0; indx < loopCount; indx++) {
                        var msgBytes = Encoding.UTF8.GetBytes($"Client{clientIndex}: a message exceeding 16 bytes, index: {indx}");
                        await client.WriteAsync(msgBytes, 0, msgBytes.Length).ConfigureAwait(false);
                        client.WaitForPipeDrain();

                        // this restarts the listener
                        await foreach (var msgSequence in client.Messages().ConfigureAwait(false)) {
                            var msg = GetString(msgSequence);
                            _output.WriteLine(msg);
                            // we expect only one message, so we end the loop
                            break;
                        }

                        // If we want to restart listening and reading we need to reset the pipeline!
                        // Note: both the pipeline reader and writer must have commpleted!
                        client.Reset();
                    }
                }
            }
#endif

            var client1Task = RunClient(1, 5);
            var client2Task = RunClient(2, 10);
            var client3Task = RunClient(3, 4);
            var client4Task = RunClient(4, 7);
            await Task.WhenAll(client1Task, client2Task, client3Task, client4Task).ConfigureAwait(false);

            // brief delay to let _output catch up
            serverCts.CancelAfter(200);
            await server1Task.ConfigureAwait(false);
            await server2Task.ConfigureAwait(false);
        }
    }
}
