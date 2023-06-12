using System.Buffers;
using System.IO.Pipes;
using PipeLines = System.IO.Pipelines;

namespace KdSoft.NamedMessagePipe
{
    /// <summary>
    /// NamedPipe client that handles message buffers without null bytes (e.g. UTF8-encoded strings).
    /// </summary>
    public class NamedMessagePipeClient
#if NETFRAMEWORK
        : NamedMessagePipeBase, IDisposable
#else
        : NamedMessagePipeBase, IDisposable, IAsyncDisposable
#endif
    {
        readonly NamedPipeClientStream _clientStream;
        readonly int _minBufferSize;

        NamedMessagePipeClient(string server, string name, int minBufferSize) : base(name) {
            this._minBufferSize = minBufferSize;
            var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
            _clientStream = new NamedPipeClientStream(server, _name, PipeDirection.InOut, pipeOptions);
        }

        /// <summary>
        /// Returns new instance of a connected <see cref="NamedMessagePipeClient"/>.
        /// </summary>
        /// <param name="server">Name of server. "." for local server.</param>
        /// <param name="name">Name of pipe.</param>
        /// <param name="minBufferSize">Minimum buffer size to use for reading messages.</param>
        /// <returns>Connected <see cref="NamedMessagePipeClient"/> instance.</returns>
        public static async Task<NamedMessagePipeClient> ConnectAsync(string server, string name, int minBufferSize = 512) {
            var result = new NamedMessagePipeClient(server, name, minBufferSize);
            try {
                await result._clientStream.ConnectAsync().ConfigureAwait(false);
            }
            catch {
                result.Dispose();
                throw;
            }
            return result;
        }

        /// <inheritdoc cref="PipeStream.WaitForPipeDrain"/>
        public void WaitForPipeDrain() {
            _clientStream.WaitForPipeDrain();
        }

        /// <summary>
        /// Resets the <see cref="PipeLines.Pipe"/> so that the client can retart reading/listening again
        /// after having processed one or more messages from the server, therefore ending the read loop.
        /// See <see cref="PipeLines.Pipe.Reset"/>
        /// </summary>
        public void Reset() {
            _pipeline.Reset();
        }

        /// <inheritdoc />
        public void Dispose() {
            _clientStream.Dispose();
        }

#if !NETFRAMEWORK
        /// <inheritdoc />
        public ValueTask DisposeAsync() {
            return _clientStream.DisposeAsync();
        }
#endif

        async Task Listen(PipeLines.PipeWriter pipelineWriter, CancellationToken cancelToken) {
#if NETFRAMEWORK
            var buffer = new byte[_minBufferSize];
#endif
            while (!cancelToken.IsCancellationRequested) {
                PipeLines.FlushResult flr;
                try {
#if NETFRAMEWORK
                    var count = await _clientStream.ReadAsync(buffer, 0, buffer.Length, cancelToken).ConfigureAwait(false);
                    var memory = new ReadOnlyMemory<byte>(buffer, 0, count);
                    pipelineWriter.Write(memory.Span);
#else
                    var memory = pipelineWriter.GetMemory(_minBufferSize);
                    var count = await _clientStream.ReadAsync(memory, cancelToken).ConfigureAwait(false);
                    pipelineWriter.Advance(count);
#endif
                    if (count == 0) {
                        pipelineWriter.Complete();
                        return;
                    }

                    if (_clientStream.IsMessageComplete) {
                        // we assume UTF8 string data, so we can use 0 as message separator
                        pipelineWriter.Write(_messageSeparator.AsSpan());
                    }

                    flr = await pipelineWriter.FlushAsync(cancelToken);
                    if (flr.IsCompleted) {
                        return;
                    }
                }
                catch (OperationCanceledException) {
                    pipelineWriter.Complete();
                    break;
                }
                catch (Exception ex) {
                    pipelineWriter.Complete(ex);
                    throw;
                }
            }
            pipelineWriter.Complete();
        }

        /// <summary>
        /// Async enumerable returning messages received.
        /// </summary>
        /// <param name="readCancelToken">Cancellation token that ends the async enumeration.</param>
        /// <remarks>
        /// When cancelled, and before the next call to <see cref="Messages(CancellationToken)"/>,
        /// it is ncessary to call <see cref="Reset"/>.
        /// </remarks>
        public IAsyncEnumerable<ReadOnlySequence<byte>> Messages(CancellationToken readCancelToken) {
            _clientStream.ReadMode = PipeTransmissionMode.Message;
            var listenTask = Listen(_pipeline.Writer, readCancelToken);
            return base.GetMessages(readCancelToken, listenTask);
        }

#if !NETFRAMEWORK
        /// <inheritdoc cref="PipeStream.WriteAsync(ReadOnlyMemory{byte}, CancellationToken)"/>
        public ValueTask WriteAsync(ReadOnlyMemory<byte> message, CancellationToken cancelToken = default) {
            return _clientStream.WriteAsync(message, cancelToken);
        }

        /// <inheritdoc cref="PipeStream.ReadAsync(Memory{byte}, CancellationToken)"/>
        public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancelToken = default) {
            return _clientStream.ReadAsync(buffer, cancelToken);
        }
#endif

        /// <inheritdoc cref="Stream.WriteAsync(byte[], int, int, CancellationToken)"/>
        public Task WriteAsync(byte[] message, int offset, int count, CancellationToken cancelToken = default) {
            return _clientStream.WriteAsync(message, offset, count, cancelToken);
        }

        /// <inheritdoc cref="Stream.ReadAsync(byte[], int, int, CancellationToken)"/>
        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken = default) {
            return _clientStream.ReadAsync(buffer, offset, count, cancelToken);
        }

        /// <inheritdoc cref="Stream.FlushAsync(CancellationToken)"/>
        public Task FlushAsync(CancellationToken cancelToken = default) {
            return _clientStream.FlushAsync(cancelToken);
        }
    }
}
