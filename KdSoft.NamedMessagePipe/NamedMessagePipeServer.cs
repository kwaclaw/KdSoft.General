using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using PipeLines = System.IO.Pipelines;

namespace KdSoft.NamedMessagePipe
{
    /// <summary>
    /// NamedPipe server that handles message buffers without null bytes (e.g. UTF8-encoded strings).
    /// </summary>
    public class NamedMessagePipeServer
#if NETFRAMEWORK
        : NamedMessagePipeBase, IDisposable
#else
        : NamedMessagePipeBase, IDisposable, IAsyncDisposable
#endif
    {
        readonly int _maxServers;
        readonly int _minBufferSize;
        /// <summary>CancellationToken that stops the server.</summary>
        readonly Task _listenTask;

        /// <summary>Internal instance of <see cref="NamedPipeServerStream"/>.</summary>
        protected NamedPipeServerStream _serverStream;
        /// <summary>CancellationToken that stops the server.</summary>
        protected readonly CancellationToken _listenCancelToken;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pipeName">Name of pipe.</param>
        /// <param name="instanceId">Unique identifier of this instance.</param>
        /// <param name="listenCancelToken">CancellationToken that stops the server.</param>
        /// <param name="maxServers">Maximum number of servers to instantiate.</param>
        /// <param name="minBufferSize">Minimum buffer size for receiving incoming messages.</param>
        public NamedMessagePipeServer(
            string pipeName,
            string instanceId,
            CancellationToken listenCancelToken,
            int maxServers = NamedPipeServerStream.MaxAllowedServerInstances,
            int minBufferSize = 512
        ) : base(pipeName, instanceId) {
            this._listenCancelToken = listenCancelToken;
            this._maxServers = maxServers;
            this._minBufferSize = minBufferSize;
            var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
            _serverStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, maxServers, PipeTransmissionMode.Message, pipeOptions);
            _listenTask = Listen(_pipeline.Writer);
        }

        /// <summary>Task representing the listening process.</summary>
        public Task ListenTask => _listenTask;

        /// <inheritdoc />
        public void Dispose() => _serverStream.Dispose();

        /// <summary>
        /// Async enumerable returning messages received.
        /// </summary>
        public IAsyncEnumerable<ReadOnlySequence<byte>> Messages() {
            return base.GetMessages(_listenCancelToken, () => _listenTask);
        }

        /// <inheritdoc cref="NamedPipeServerStream.Disconnect"/>
        public void Disconnect() {
            _serverStream.Disconnect();
        }

        /// <inheritdoc cref="PipeStream.Read(byte[], int, int)"/>
        public int Read(byte[] buffer, int offset, int count) {
            return _serverStream.Read(buffer, offset, count);
        }

        /// <inheritdoc cref="PipeStream.Write(byte[], int, int)"/>
        public void Write(byte[] buffer, int offset, int count) {
            _serverStream.Write(buffer, offset, count);
        }

        /// <inheritdoc cref="PipeStream.Flush"/>
        public void Flush() {
            _serverStream.Flush();
        }

        async Task Listen(PipeLines.PipeWriter pipelineWriter) {
#if NETFRAMEWORK
            var buffer = new byte[_minBufferSize];
#endif
            while (!_listenCancelToken.IsCancellationRequested) {
                try {
#if NETFRAMEWORK
                    //BUG: on .NET Framework, WaitForConnectionAsync cannot be cancelled!
                    await MakeCancellable(() => WaitForConnection(), _listenCancelToken).ConfigureAwait(false);
#else
                    await WaitForConnectionAsync().ConfigureAwait(false);
#endif
                    PipeLines.FlushResult writeResult = default;
                    int byteCount = 0;
                    while (!_listenCancelToken.IsCancellationRequested) {
                        NamedPipeEventSource.Log.ListenBeginRead(nameof(NamedMessagePipeServer), PipeName, InstanceId);
#if NETFRAMEWORK
                        byteCount = await MakeCancellable(() => _serverStream.Read(buffer, 0, buffer.Length), _listenCancelToken).ConfigureAwait(false);
                        NamedPipeEventSource.Log.ListenEndRead(nameof(NamedMessagePipeServer), PipeName, InstanceId);
                        var memory = new ReadOnlyMemory<byte>(buffer, 0, byteCount);
                        await pipelineWriter.WriteAsync(memory, _listenCancelToken).ConfigureAwait(false);
#else
                        var memory = pipelineWriter.GetMemory(_minBufferSize);
                        byteCount = await _serverStream.ReadAsync(memory, _listenCancelToken).ConfigureAwait(false);
                        NamedPipeEventSource.Log.ListenEndRead(nameof(NamedMessagePipeServer), PipeName, InstanceId);
                        pipelineWriter.Advance(byteCount);
#endif

                        if (byteCount == 0) {
                            break;
                        }

                        if (_serverStream.IsMessageComplete) {
                            // we assume UTF8 string data, so we can use 0 as message separator
                            await pipelineWriter.WriteAsync(_messageSeparator, _listenCancelToken).ConfigureAwait(false);
                        }
                        writeResult = await pipelineWriter.FlushAsync(_listenCancelToken).ConfigureAwait(false);
                        if (writeResult.IsCompleted || writeResult.IsCanceled) {
                            break;
                        }
                    }
                    NamedPipeEventSource.Log.ServerDisconnectedFromClient(PipeName, InstanceId, byteCount == 0);
                }
                catch (OperationCanceledException) {
                    NamedPipeEventSource.Log.ListenCancel(nameof(NamedMessagePipeServer), PipeName, InstanceId);
                    break;
                }
                catch (Exception ex) {
                    NamedPipeEventSource.Log.ListenError(nameof(NamedMessagePipeServer), PipeName, InstanceId, ex);
                    break;
                }
                finally {
                    NamedPipeEventSource.Log.ListenEnd(nameof(NamedMessagePipeServer), PipeName, InstanceId);
                    if (_serverStream.IsConnected) {
                        try { _serverStream.Disconnect(); }
                        catch { }
                    }
                }
            }
        }

#if NETFRAMEWORK
        void WaitForConnection(int retries = 1) {
            do {
                try {
                    NamedPipeEventSource.Log.ServerWaitForConnection(PipeName, InstanceId);
                    _serverStream.WaitForConnection();
                    NamedPipeEventSource.Log.ServerConnectedToClient(PipeName, InstanceId);
                    return;
                }
                catch (IOException ex) {
                    NamedPipeEventSource.Log.ServerWaitForConnectionError(PipeName, InstanceId, ex);
                    var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
                    var newServerStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, _maxServers, PipeTransmissionMode.Message, pipeOptions);
                    var oldServerStream = Interlocked.Exchange(ref _serverStream, newServerStream);
                    try { oldServerStream.Dispose(); }
                    catch { }
                }
            } while (retries-- > 0);
        }

        /// <inheritdoc cref="Stream.WriteAsync(byte[], int, int, CancellationToken)"/>
        public Task WriteAsync(byte[] message, int offset, int count, CancellationToken cancelToken = default) {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _listenCancelToken);
            return MakeCancellable(() => _serverStream.Write(message, offset, count), cts.Token);
        }

        /// <inheritdoc cref="Stream.FlushAsync(CancellationToken)"/>
        public Task FlushAsync(CancellationToken cancelToken = default) {
            return MakeCancellable(() => _serverStream.Flush(), cancelToken);
        }
#else
        /// <inheritdoc />
        public ValueTask DisposeAsync() => _serverStream.DisposeAsync();

        async Task WaitForConnectionAsync(int retries = 1) {
            do {
                try {
                    NamedPipeEventSource.Log.ServerWaitForConnection(PipeName, InstanceId);
                    await _serverStream.WaitForConnectionAsync(_listenCancelToken).ConfigureAwait(false);
                    NamedPipeEventSource.Log.ServerConnectedToClient(PipeName, InstanceId);
                    return;
                }
                catch (IOException ex) {
                    NamedPipeEventSource.Log.ServerWaitForConnectionError(PipeName, InstanceId, ex);
                    var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
                    var newServerStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, _maxServers, PipeTransmissionMode.Message, pipeOptions);
                    var oldServerStream = Interlocked.Exchange(ref _serverStream, newServerStream);
                    try { await oldServerStream.DisposeAsync().ConfigureAwait(false); }
                    catch { }
                }
            } while (retries-- > 0);
        }

        /// <summary>
        /// Write message buffer to current connection.
        /// See <see cref="PipeStream.WriteAsync(ReadOnlyMemory{byte}, CancellationToken)"/>.
        /// </summary>
        /// <param name="message">Message buffer, must not contain null bytes.</param>
        /// <param name="cancelToken">Cancels write operation.</param>
        /// <returns></returns>
        public ValueTask WriteAsync(ReadOnlyMemory<byte> message, CancellationToken cancelToken = default) {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _listenCancelToken);
            return _serverStream.WriteAsync(message, cts.Token);
        }

        /// <inheritdoc cref="Stream.WriteAsync(byte[], int, int, CancellationToken)"/>
        public Task WriteAsync(byte[] message, int offset, int count, CancellationToken cancelToken = default) {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _listenCancelToken);
            return _serverStream.WriteAsync(message, offset, count, cts.Token);
        }

        /// <inheritdoc cref="Stream.FlushAsync(CancellationToken)"/>
        public Task FlushAsync(CancellationToken cancelToken = default) {
            return _serverStream.FlushAsync(cancelToken);
        }
#endif
    }
}
