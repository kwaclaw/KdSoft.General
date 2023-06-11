
using System;
using System.IO.Pipes;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace KdSoft.NamedPipe.Tests
{
    [Collection("Library Tests")]
    public class NamedPipeTests
    {
        readonly ITestOutputHelper _output;
        public const string PipeName = "elekta-pipe-test";

        public NamedPipeTests(ITestOutputHelper output) {
            this._output = output;
        }

        [Fact]
        public async Task MessageClientSend() {
            using var cts = new CancellationTokenSource();
            using var server = new NamedPipeMessageServer(PipeName, cts.Token, 16);

            var readTask = Task.Run(async () => {
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = Encoding.UTF8.GetString(msgSequence);
                    _output.WriteLine(msg);
                }
                _output.WriteLine("End of messages");
            });

            using var client = await NamedPipeMessageClient.ConnectAsync(".", PipeName).ConfigureAwait(false);
            for (int indx = 0; indx < 10; indx++) {
                await client.WriteAsync(Encoding.UTF8.GetBytes($"A long message exceeding 16 bytes, index: {indx}")).ConfigureAwait(false);
            }

            client.WaitForPipeDrain();
            await client.DisposeAsync().ConfigureAwait(false);

            // brief delay to let _output catch up
            cts.CancelAfter(100);
            await readTask.ConfigureAwait(false);
        }

        [Fact]
        public async Task MessageServerSend() {
            using var serverCts = new CancellationTokenSource();
            using var server = new NamedPipeMessageServer(PipeName, serverCts.Token, 16);

            // server listens for incoming messages and replies with a number of messages
            var serverTask = Task.Run(async () => {
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = Encoding.UTF8.GetString(msgSequence);
                    _output.WriteLine(msg);
                    for (int indx = 0; indx < 10; indx++) {
                        await server.WriteAsync(Encoding.UTF8.GetBytes($"A long message exceeding 16 bytes, index: {indx}")).ConfigureAwait(false);
                    }
                    await server.WriteAsync(Encoding.UTF8.GetBytes("Last Message")).ConfigureAwait(false);
                }
                _output.WriteLine("End of messages");
            });

            using var client = await NamedPipeMessageClient.ConnectAsync(".", PipeName).ConfigureAwait(false);
            await client.WriteAsync(Encoding.UTF8.GetBytes($"Hello server")).ConfigureAwait(false);
            client.WaitForPipeDrain();

            // the client's listening loop will only end when the client is disposed, as this triggers the read/listen cancellation token;
            // it depends on the server sending the termination message
            var clientCts = new CancellationTokenSource();
            await foreach (var msgSequence in client.Messages(clientCts.Token).ConfigureAwait(false)) {
                var msg = Encoding.UTF8.GetString(msgSequence);
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
        public async Task MessageSendReply() {
            using var serverCts = new CancellationTokenSource();
            using var server = new NamedPipeMessageServer(PipeName, serverCts.Token, 16);

            var serverTask = Task.Run(async () => {
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = Encoding.UTF8.GetString(msgSequence);
                    _output.WriteLine(msg);
                    var reply = Encoding.UTF8.GetBytes($"Reply to {msg}");
                    await server.WriteAsync(reply).ConfigureAwait(false);
                }
            });

            using var client = await NamedPipeMessageClient.ConnectAsync(".", PipeName).ConfigureAwait(false);
            for (int indx = 0; indx < 10; indx++) {
                await client.WriteAsync(Encoding.UTF8.GetBytes($"A very long message exceeding 16 bytes, index: {indx}")).ConfigureAwait(false);
                client.WaitForPipeDrain();

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
                    var msg = Encoding.UTF8.GetString(msgSequence);
                    _output.WriteLine(msg);
                    // we expect only one message, so we end the loop
                    clientCts.Cancel();
                }

                //await client.ListenTask.ConfigureAwait(false);

                // If we want to restart listening and reading we need to reset the pipeline!
                // Note: both the pipeline reader and writer must have commpleted!
                client.Reset();
            }

            // brief delay to let _output catch up
            serverCts.CancelAfter(100);
            await serverTask.ConfigureAwait(false);
        }

#if false
        [Fact]
        public async Task MessageServerSend() {
            using var cts = new CancellationTokenSource();
            using var server = new NamedPipeMessageServer(PipeName, cts.Token, 16);

            // server listens for incoming messages and replies with a number of messages
            var serverTask = Task.Run(async () => {
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = Encoding.UTF8.GetString(msgSequence);
                    _output.WriteLine(msg);
                    for (int indx = 0; indx < 10; indx++) {
                        await server.WriteAsync(Encoding.UTF8.GetBytes($"A long message exceeding 16 bytes, index: {indx}")).ConfigureAwait(false);
                    }
                    await server.WriteAsync(Encoding.UTF8.GetBytes("Last Message")).ConfigureAwait(false);
                }
                _output.WriteLine("End of messages");
            });

            using var client = await NamedPipeMessageClient.ConnectAsync(".", PipeName).ConfigureAwait(false);
            await client.WriteAsync(Encoding.UTF8.GetBytes($"Hello server")).ConfigureAwait(false);
            client.WaitForPipeDrain();

            // the client's listening loop will only end when the client is disposed, as this triggers the read/listen cancellation token;
            // it depends on the server sending the termination message
            await foreach (var msgSequence in client.Messages().ConfigureAwait(false)) {
                var msg = Encoding.UTF8.GetString(msgSequence);
                _output.WriteLine(msg);
                if (msg == "Last Message") {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
            }

            try {
                // brief delay to let _output catch up
                cts.CancelAfter(100);
                await serverTask.ConfigureAwait(false);
            }
            catch (Exception ex) {
                _output.WriteLine(ex.ToString());
            }
        }

        [Fact]
        public async Task MessageSendReply() {
            using var cts = new CancellationTokenSource();
            using var server = new NamedPipeMessageServer(PipeName, cts.Token, 16);

            var serverTask = Task.Run(async () => {
                await foreach (var msgSequence in server.Messages().ConfigureAwait(false)) {
                    var msg = Encoding.UTF8.GetString(msgSequence);
                    _output.WriteLine(msg);
                    var reply = Encoding.UTF8.GetBytes($"Reply to {msg}");
                    await server.WriteAsync(reply).ConfigureAwait(false);
                }
            });

            for (int indx = 0; indx < 10; indx++) {
                using var client = await NamedPipeMessageClient.ConnectAsync(".", PipeName).ConfigureAwait(false);
                await client.WriteAsync(Encoding.UTF8.GetBytes($"A very long message exceeding 16 bytes, index: {indx}")).ConfigureAwait(false);
                client.WaitForPipeDrain();

                // we can only end the listening loop by disposing the client, otherwise the loop will hang
                var iterator = client.Messages().GetAsyncEnumerator();
                if (await iterator.MoveNextAsync().ConfigureAwait(false)) {
                    var msgSequence = iterator.Current;
                    var msg = Encoding.UTF8.GetString(msgSequence);
                    _output.WriteLine(msg);
                    await client.DisposeAsync().ConfigureAwait(false);
                }
                await iterator.DisposeAsync().ConfigureAwait(false);

                // we can only end the listening loop by disposing the client, otherwise the loop will hang
                //await foreach (var msgSequence in client.Messages().ConfigureAwait(false)) {
                //    var msg = Encoding.UTF8.GetString(msgSequence);
                //    _output.WriteLine(msg);
                //    await client.DisposeAsync().ConfigureAwait(false);
                //}
            }

            // brief delay to let _output catch up
            cts.CancelAfter(100);
            await serverTask.ConfigureAwait(false);
        }
#endif
    }
}
