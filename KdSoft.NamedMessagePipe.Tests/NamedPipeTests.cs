using System;
using System.Buffers;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
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
        const int ConnectTimeout = 1000;
        const int ServerCancelDelay = 500;
        const TaskCreationOptions ServerTaskOptions = TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously;

        readonly ITestOutputHelper _output;

        public const string PipeName = "kdsoft-pipe-test";


#if NETFRAMEWORK
        readonly NamedPipeTestFixtureFramework _fixture;

        public NamedPipeTests(ITestOutputHelper output, NamedPipeTestFixtureFramework fixture) {
            this._output = output;
            this._fixture = fixture;

        }
#else
        readonly NamedPipeTestFixtureCore _fixture;

        public NamedPipeTests(ITestOutputHelper output, NamedPipeTestFixtureCore fixture) {
            this._output = output;
            this._fixture = fixture;
        }
#endif

        string GetString(ReadOnlySequence<byte> sequence) {
#if NETCOREAPP3_1_OR_GREATER || NETFRAMEWORK
            return Encoding.UTF8.GetString(sequence.ToArray());
#else
            return Encoding.UTF8.GetString(msgSequence);
#endif
        }

        async Task WriteMessage(NamedMessagePipeServer server, string msg) {
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            try {
                var count = Encoding.UTF8.GetBytes(msg, 0, msg.Length, buffer, 0);
                buffer[count++] = 0;
                await server.Stream.WriteAsync(buffer, 0, count);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        async Task WriteMessage(NamedMessagePipeClient client, string msg) {
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            try {
                var count = Encoding.UTF8.GetBytes(msg, 0, msg.Length, buffer, 0);
                // 0 is the message terminator
                buffer[count++] = 0;
                await client.Stream.WriteAsync(buffer, 0, count);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        [Fact]
        public async Task ClientSendMessages() {
            NamedPipeEventSource.Log.Write(nameof(ClientSendMessages));

            using var cts = new CancellationTokenSource();

            async Task RunServer() {
                using var server = new NamedMessagePipeServer(PipeName, nameof(ClientSendMessages) + "-Server", cts.Token, 2);
                await foreach (var msgSequence in server.Messages()) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                }
                _output.WriteLine("End of messages");
            }
            var serverTask = RunServer();

            using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(ClientSendMessages) + "-Client", ConnectTimeout);
            for (int indx = 0; indx < 10; indx++) {
                await WriteMessage(client, $"A long message exceeding 16 bytes, index: {indx}");
            }
            await client.Stream.FlushAsync();
            //client.WaitForPipeDrain();

            // brief delay to let _output catch up
            cts.CancelAfter(ServerCancelDelay);
            await serverTask;

            _output.WriteLine($"Finished {nameof(ClientSendMessages)}");

            // give Windows time to release the named pipe
            await Task.Delay(2000);
        }

#if NETFRAMEWORK || NET6_0_OR_GREATER
        [Fact]
#if NET6_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        public async Task SecuredClientSendMessages() {
            NamedPipeEventSource.Log.Write(nameof(SecuredClientSendMessages));

            using var cts = new CancellationTokenSource();

            var pipeSecurity = new PipeSecurity();
            var id = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            // Allow everyone read and write access to the pipe.
            pipeSecurity.SetAccessRule(new PipeAccessRule(id, PipeAccessRights.ReadWrite, AccessControlType.Allow));

            using var server = new NamedMessagePipeServer(PipeName, nameof(ClientSendMessages) + "-Server", cts.Token, 2, pipeSecurity);

            var readTask = Task.Run(async () => {
                await foreach (var msgSequence in server.Messages()) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                }
                _output.WriteLine("End of messages");
            });

            using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(ClientSendMessages) + "-Client", ConnectTimeout);
            for (int indx = 0; indx < 10; indx++) {
                await WriteMessage(client, $"A long message exceeding 16 bytes, index: {indx}");
            }
            await client.Stream.FlushAsync();
            //client.WaitForPipeDrain();
#if NET6_0_OR_GREATER
            await client.DisposeAsync();
#else
            client.Dispose();
#endif

            // brief delay to let _output catch up
            cts.CancelAfter(ServerCancelDelay);
            await readTask;

            _output.WriteLine($"Finished {nameof(ClientSendMessages)}");

            // give Windows time to release the named pipe
            await Task.Delay(2000);
        }
#endif

        [Fact]
        public async Task MultipleClientSendMessages() {
            using var cts = new CancellationTokenSource();

            async Task RunServer(int serverIndex) {
                using var server = new NamedMessagePipeServer(PipeName, $"{nameof(MultipleClientSendMessages)}-Server{serverIndex}", cts.Token, 2);
                await foreach (var msgSequence in server.Messages()) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                }
                _output.WriteLine($"Server{serverIndex}: End of messages");
            }

            async Task RunClient(int clientIndex) {
                using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(MultipleClientSendMessages) + "-Client" + clientIndex, ConnectTimeout);
                for (int indx = 0; indx < 10; indx++) {
                    await WriteMessage(client, $"Client{clientIndex}: a message exceeding 16 bytes, index: {indx}");
                }
                await client.Stream.FlushAsync();
                //client.WaitForPipeDrain();
            }

            var server1Task = RunServer(1);
            var server2Task = RunServer(2);

            var client1Task = RunClient(1);
            var client2Task = RunClient(2);
            var client3Task = RunClient(3);
            var client4Task = RunClient(4);
            await Task.WhenAll(client1Task, client2Task, client3Task, client4Task);

            // brief delay to let _output catch up
            cts.CancelAfter(ServerCancelDelay);
            await server1Task;
            await server2Task;

            _output.WriteLine($"Finished {nameof(MultipleClientSendMessages)}");

            // give Windows time to release the named pipe
            await Task.Delay(2000);
        }

        [Fact]
        public async Task ServerSendMessages() {
            using var serverCts = new CancellationTokenSource();

            // Workaround: for some reason, when multiple tests with the same pipe name are run, 
            //             the server in this test does not start listening, hangs there while
            //             the client seems to connect to a different, not yet disposed, instance.
            // So we use a different pipe name here!
            var pipeName = PipeName + "_special";

            // server listens for incoming messages and replies with a number of messages
            async Task RunServer() {
                using var server = new NamedMessagePipeServer(pipeName, nameof(ServerSendMessages) + "-Server", serverCts.Token, 2);
                await foreach (var msgSequence in server.Messages()) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    for (int indx = 0; indx < 10; indx++) {
                        await WriteMessage(server, $"A long message exceeding 16 bytes, index: {indx}");
                    }
                    await WriteMessage(server, "Last Message");
                    await server.Stream.FlushAsync();
                }
                _output.WriteLine("End of messages");
            }
            var serverTask = RunServer();

            using var client = await NamedMessagePipeClient.ConnectAsync(".", pipeName, nameof(ServerSendMessages) + "-Client", ConnectTimeout);
            await WriteMessage(client, "Hello from client");
            //client.WaitForPipeDrain();

            // the client's listening loop will only end when the server sends the termination message
            await foreach (var msgSequence in client.Messages()) {
                var msg = GetString(msgSequence);
                _output.WriteLine(msg);
                if (msg == "Last Message") {
                    break;
                }
            }

            // brief delay to let _output catch up
            serverCts.CancelAfter(ServerCancelDelay);
            await serverTask;

            _output.WriteLine($"Finished {nameof(ServerSendMessages)}");

            // give Windows time to release the named pipe
            await Task.Delay(2000);
        }

        [Fact]
        public async Task SendReplyMessage() {
            using var serverCts = new CancellationTokenSource();

            async Task RunServer() {
                using var server = new NamedMessagePipeServer(PipeName, nameof(SendReplyMessage) + "-Server", serverCts.Token, 2);
                await foreach (var msgSequence in server.Messages()) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    await WriteMessage(server, $"Reply to {msg}");
                    await server.Stream.FlushAsync();
                }
                _output.WriteLine("Server: End of messages");
            }
            var serverTask = RunServer();

            async Task RunClient() {
#if !NETFRAMEWORK
                using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(SendReplyMessage) + "-Client", ConnectTimeout);
#endif
                for (int indx = 0; indx < 10; indx++) {
#if NETFRAMEWORK
                    // in full framework we can only use Dispose() to stop the client from reading, cancellation does not work
                    using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(SendReplyMessage) + "-Client", ConnectTimeout);
#endif
                    await WriteMessage(client, $"A nice Hello from client {indx}");
                    await client.Stream.FlushAsync();
                    //client.WaitForPipeDrain();

                    // this restarts the listener
                    //var iterator = client.Messages().GetAsyncEnumerator();
                    //while (await iterator.MoveNextAsync()) {
                    //    var msgSequence = iterator.Current;
                    //    var msg = GetString(msgSequence);
                    //    _output.WriteLine(msg);
                    //    // we expect only one message, so we end the loop
                    //    break;
                    //}
                    //await iterator.DisposeAsync();

                    // this restarts the listener
                    await foreach (var msgSequence in client.Messages()) {
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
            }
            var clientTask = RunClient();
            await clientTask;

            // brief delay to let _output catch up
            serverCts.CancelAfter(ServerCancelDelay);
            await serverTask;

            _output.WriteLine($"Finished {nameof(SendReplyMessage)}");

            // give Windows time to release the named pipe
            await Task.Delay(2000);
        }

        [Fact]
        public async Task MultipleClientSendReplyMessage() {
            using var serverCts = new CancellationTokenSource();

            var server1Task = Task.Factory.StartNew(async () => {
                using var server1 = new NamedMessagePipeServer(PipeName, nameof(MultipleClientSendReplyMessage) + "-Server1", serverCts.Token, 2);
                await foreach (var msgSequence in server1.Messages()) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    await WriteMessage(server1, $"Server1 reply to {msg}");
                    await server1.Stream.FlushAsync();
                }
                _output.WriteLine("Server1: End of messages");
            }, ServerTaskOptions);

            var server2Task = Task.Factory.StartNew(async () => {
                using var server2 = new NamedMessagePipeServer(PipeName, nameof(MultipleClientSendReplyMessage) + "-Server2", serverCts.Token, 2);
                await foreach (var msgSequence in server2.Messages()) {
                    var msg = GetString(msgSequence);
                    _output.WriteLine(msg);
                    await WriteMessage(server2, $"Server2 reply to {msg}");
                    await server2.Stream.FlushAsync();
                }
                _output.WriteLine("Server2: End of messages");
            }, ServerTaskOptions);

#if NETFRAMEWORK
            async Task RunClient(int clientIndex, int loopCount) {
                for (int indx = 0; indx < loopCount; indx++) {
                    // in full framework we can only use Dispose() to stop the client from reading, Cancellation and Reset do not work
                    using var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(MultipleClientSendReplyMessage) + "-Client" + clientIndex, ConnectTimeout);
                    await WriteMessage(client, $"Client{clientIndex}: a nice Hello, index: {indx}");
                    await client.FlushAsync();
                    client.Stream.WaitForPipeDrain();

                    // this starts the listener
                    await foreach (var msgSequence in client.Messages()) {
                        var msg = GetString(msgSequence);
                        _output.WriteLine(msg);
                        // we expect only one message, so we end the loop
                        break;
                    }
                }
            }
#else
            async Task RunClient(int clientIndex, int loopCount) {
                using (var client = await NamedMessagePipeClient.ConnectAsync(".", PipeName, nameof(MultipleClientSendReplyMessage) + "-Client" + clientIndex, ConnectTimeout)) {
                    for (int indx = 0; indx < loopCount; indx++) {
                        await WriteMessage(client, $"Client{clientIndex}: a nice Hello, index: {indx}");
                        await client.Stream.FlushAsync();
                        //client.WaitForPipeDrain();

                        // this restarts the listener
                        await foreach (var msgSequence in client.Messages()) {
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
            await Task.WhenAll(client1Task, client2Task, client3Task, client4Task);

            // brief delay to let _output catch up
            serverCts.CancelAfter(ServerCancelDelay);
            await server1Task;
            await server2Task;

            _output.WriteLine($"Finished {nameof(MultipleClientSendReplyMessage)}");

            // give Windows time to release the named pipe
            await Task.Delay(2000);
        }
    }
}
