using System.Buffers;
using System.IO.Pipes;
using PipeLines = System.IO.Pipelines;

namespace KdSoft.NamedMessagePipe
{
    /// <summary>
    /// NamedPipe server that handles message buffers without null bytes (e.g. UTF8-encoded strings).
    /// </summary>
    public class NamedMessagePipeServer: NamedMessagePipeBase, IDisposable, IAsyncDisposable
    {
        readonly NamedPipeServerStream _server;
        readonly int _minBufferSize;
        /// <summary>CancellationToken that stops the server.</summary>
        protected readonly CancellationToken _listenCancelToken;
        /// <summary>CancellationToken that stops the server.</summary>
        protected readonly Task _listenTask;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of pipe.</param>
        /// <param name="listenCancelToken">CancellationToken that stops the server.</param>
        /// <param name="maxServers">Maximum number of servers to instantiate.</param>
        /// <param name="minBufferSize">Minimum buffer size for receiving incoming messages.</param>
        public NamedMessagePipeServer(
            string name,
            CancellationToken listenCancelToken, 
            int maxServers = NamedPipeServerStream.MaxAllowedServerInstances,
            int minBufferSize = 512
        ) : base(name)
        {
            var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
            _server = new NamedPipeServerStream(_name, PipeDirection.InOut, maxServers, PipeTransmissionMode.Message, pipeOptions);
            this._listenCancelToken = listenCancelToken;
            this._minBufferSize = minBufferSize;
            _listenTask = Listen(_pipeline.Writer);
        }

        /// <summary>Task representing the listening process.</summary>
        public Task ListenTask => _listenTask;

        /// <inheritdoc />
        public void Dispose() => _server.Dispose();

        /// <inheritdoc />
        public ValueTask DisposeAsync() => _server.DisposeAsync();

        async Task Listen(PipeLines.PipeWriter pipelineWriter) {
            while (!_listenCancelToken.IsCancellationRequested) {
                try {
                    await _server.WaitForConnectionAsync(_listenCancelToken).ConfigureAwait(false);

                    while (!_listenCancelToken.IsCancellationRequested) {
                        var memory = pipelineWriter.GetMemory(_minBufferSize);
                        var count = await _server.ReadAsync(memory, _listenCancelToken).ConfigureAwait(false);
                        if (count == 0) {
                            break;
                        }

                        pipelineWriter.Advance(count);

                        if (_server.IsMessageComplete) {
                            // we assume UTF8 string data, so we can use 0 as message separator
                            pipelineWriter.Write(_messageSeparator.AsSpan());
                        }

                        var flr = await pipelineWriter.FlushAsync(_listenCancelToken).ConfigureAwait(false);
                        if (flr.IsCompleted) {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (Exception ex) {
                    //TODO how to log this exception?
                    break;
                }
                finally {
                    if (_server.IsConnected)
                        _server.Disconnect();
                }
            }
        }

        /// <summary>
        /// Async enumerable returning messages received.
        /// </summary>
        public IAsyncEnumerable<ReadOnlySequence<byte>> Messages() {
            return base.GetMessages(_listenCancelToken, _listenTask);
        }

        /// <inheritdoc cref="NamedPipeServerStream.Disconnect"/>
        public void Disconnect() {
            _server.Disconnect();
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
            return _server.WriteAsync(message, cts.Token);
        }
    }
}
