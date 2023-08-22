using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Versioning;
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
        readonly Task _listenTask;

        /// <summary>Internal instance of <see cref="NamedPipeServerStream"/>.</summary>
        protected NamedPipeServerStream _serverStream;
        /// <summary>CancellationToken that stops the server.</summary>
        protected readonly CancellationToken _listenCancelToken;

#if NETFRAMEWORK
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeName">Name of pipe.</param>
        /// <param name="instanceId">Unique identifier of this instance.</param>
        /// <param name="listenCancelToken">CancellationToken that stops the server.</param>
        /// <param name="maxServers">Maximum number of servers to instantiate.</param>
        /// <param name="minBufferSize">Minimum buffer size for receiving incoming messages.</param>
        /// <param name="security">Access rights for named pipe.</param>
        public NamedMessagePipeServer(
            string pipeName,
            string instanceId,
            CancellationToken listenCancelToken,
            int maxServers = NamedPipeServerStream.MaxAllowedServerInstances,
            int minBufferSize = 512,
            PipeSecurity? security = null
        ) : base(pipeName, instanceId) {
            this._listenCancelToken = listenCancelToken;
            this._maxServers = maxServers;
            this._minBufferSize = minBufferSize;

            var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
            _serverStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, maxServers, PipeTransmissionMode.Byte, pipeOptions, 0, 0, security);
            
            _listenTask = Listen(_pipeline.Writer);
            listenCancelToken.Register(() => {
                if (_serverStream.IsConnected) {
                    try { _serverStream.Disconnect(); }
                    catch { }
                }
            });
        }
#elif NET6_0_OR_GREATER
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeName">Name of pipe.</param>
        /// <param name="instanceId">Unique identifier of this instance.</param>
        /// <param name="listenCancelToken">CancellationToken that stops the server.</param>
        /// <param name="maxServers">Maximum number of servers to instantiate.</param>
        /// <param name="minBufferSize">Minimum buffer size for receiving incoming messages.</param>
        /// <param name="security">Access rights for named pipe.</param>
        [SupportedOSPlatform("windows")]
        public NamedMessagePipeServer(
            string pipeName,
            string instanceId,
            CancellationToken listenCancelToken,
            PipeSecurity security,
            int maxServers = NamedPipeServerStream.MaxAllowedServerInstances,
            int minBufferSize = 512
        ) : base(pipeName, instanceId) {
            this._listenCancelToken = listenCancelToken;
            this._maxServers = maxServers;
            this._minBufferSize = minBufferSize;

            var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
            _serverStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, maxServers, PipeTransmissionMode.Byte, pipeOptions, 0, 0);
            _serverStream.SetAccessControl(security);

            _listenTask = Listen(_pipeline.Writer);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeName">Name of pipe.</param>
        /// <param name="instanceId">Unique identifier of this instance.</param>
        /// <param name="listenCancelToken">CancellationToken that stops the server.</param>
        /// <param name="maxServers">Maximum number of servers to instantiate.</param>
        /// <param name="minBufferSize">Minimum buffer size for receiving incoming messages.</param>
        [UnsupportedOSPlatform("windows")]
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
            _serverStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, maxServers, PipeTransmissionMode.Byte, pipeOptions, 0, 0);

            _listenTask = Listen(_pipeline.Writer);
        }
#else
        /// <summary>
        /// Constructor.
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
            _serverStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, maxServers, PipeTransmissionMode.Byte, pipeOptions, 0, 0);

            _listenTask = Listen(_pipeline.Writer);
        }
#endif

        /// <inheritdoc cref="NamedPipeServerStream"/>
        public bool IsConnected { get => _serverStream.IsConnected; }

        /// <summary>Task representing the listening process.</summary>
        public Task ListenTask => _listenTask;

        /// <summary>Implementation of Dispose pattern.</summary>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                _serverStream.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Async enumerable returning messages received.
        /// </summary>
        public IAsyncEnumerable<ReadOnlySequence<byte>> Messages() {
            return base.GetMessages(CancellationToken.None, () => _listenTask);;
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
                    //BUG: on .NET Framework, WaitForConnectionAsync cannot be cancelled properly!
                    //     It might work to call _ServerStream.Disconnect().
                    await _serverStream.WaitForConnectionAsync(_listenCancelToken).ConfigureAwait(false);

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

                        writeResult = await pipelineWriter.FlushAsync(_listenCancelToken).ConfigureAwait(false);
                        if (writeResult.IsCompleted || writeResult.IsCanceled) {
                            break;
                        }
                    }
                    NamedPipeEventSource.Log.ServerDisconnectedFromClient(PipeName, InstanceId, byteCount == 0);
                }
                catch (OperationCanceledException) {
                    NamedPipeEventSource.Log.ListenCancel(nameof(NamedMessagePipeServer), PipeName, InstanceId);
                    // this ends the  listen loop
                    await pipelineWriter.CompleteAsync().ConfigureAwait(false);
                    break;
                }
                catch (IOException ioex) {
                    NamedPipeEventSource.Log.ServerConnectionError(PipeName, InstanceId, ioex);
                    // we must call Disconnect without checking _serverStream.IsConnected, so we can't let the finally clause handle it
                    _serverStream.Disconnect();
                    // we continue the listen loop
                    continue;
                }
                catch (Exception ex) {
                    NamedPipeEventSource.Log.ListenError(nameof(NamedMessagePipeServer), PipeName, InstanceId, ex);
                    // this ends the  listen loop
                    await pipelineWriter.CompleteAsync().ConfigureAwait(false);
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
        /// <inheritdoc cref="Stream.WriteAsync(byte[], int, int, CancellationToken)"/>
        public Task WriteAsync(byte[] message, int offset, int count, CancellationToken cancelToken = default) {
            return MakeCancellable(() => _serverStream.Write(message, offset, count), cancelToken);
        }

        /// <inheritdoc cref="Stream.FlushAsync(CancellationToken)"/>
        public Task FlushAsync(CancellationToken cancelToken = default) {
            return MakeCancellable(() => _serverStream.Flush(), cancelToken);
        }
#else
        /// <inheritdoc />
        public ValueTask DisposeAsync() => _serverStream.DisposeAsync();

        /// <summary>
        /// Write message buffer to current connection.
        /// See <see cref="PipeStream.WriteAsync(ReadOnlyMemory{byte}, CancellationToken)"/>.
        /// </summary>
        /// <param name="message">Message buffer, must not contain null bytes.</param>
        /// <param name="cancelToken">Cancels write operation.</param>
        /// <returns></returns>
        public ValueTask WriteAsync(ReadOnlyMemory<byte> message, CancellationToken cancelToken = default) {
            return _serverStream.WriteAsync(message, cancelToken);
        }

        /// <inheritdoc cref="PipeStream.WriteAsync(byte[], int, int, CancellationToken)"/>
        public Task WriteAsync(byte[] message, int offset, int count, CancellationToken cancelToken = default) {
            return _serverStream.WriteAsync(message, offset, count, cancelToken);
        }

#if NETSTANDARD2_1
        /// <inheritdoc cref="Stream.FlushAsync(CancellationToken)"/>
        public Task FlushAsync(CancellationToken cancelToken = default) {
            return MakeCancellable(() => _serverStream.Flush(), cancelToken);
        }
#else
        /// <inheritdoc cref="PipeStream.FlushAsync(CancellationToken)"/>
        public Task FlushAsync(CancellationToken cancelToken = default) {
            return _serverStream.FlushAsync(cancelToken);
        }
#endif

#endif
    }
}
