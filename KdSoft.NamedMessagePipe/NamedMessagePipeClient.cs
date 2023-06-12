using System.Buffers;
using System.IO.Pipes;
using PipeLines = System.IO.Pipelines;

namespace KdSoft.NamedMessagePipe
{
    /// <summary>
    /// NamedPipe client that handles message buffers without null bytes (e.g. UTF8-encoded strings).
    /// </summary>
    public class NamedMessagePipeClient
        : NamedMessagePipeBase, IDisposable, IAsyncDisposable
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

        /// <inheritdoc />
        public ValueTask DisposeAsync() {
            return _clientStream.DisposeAsync();
        }

        async Task Listen(PipeLines.PipeWriter pipelineWriter, CancellationToken cancelToken) {
            while (!cancelToken.IsCancellationRequested) {
                PipeLines.FlushResult flr;
                try {
                    var memory = pipelineWriter.GetMemory(_minBufferSize);
                    var count = await _clientStream.ReadAsync(memory, cancelToken).ConfigureAwait(false);
                    if (count == 0) {
                        pipelineWriter.Complete();
                        return;
                    }

                    pipelineWriter.Advance(count);

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

        /// <inheritdoc cref="PipeStream.WriteAsync(ReadOnlyMemory{byte}, CancellationToken)"/>
        public ValueTask WriteAsync(ReadOnlyMemory<byte> message, CancellationToken cancelToken = default) {
            return _clientStream.WriteAsync(message, cancelToken);
        }

        /// <inheritdoc cref="Stream.FlushAsync(CancellationToken)"/>
        public Task FlushAsync(CancellationToken cancelToken = default) {
            return _clientStream.FlushAsync(cancelToken);
        }

        /// <inheritdoc cref="PipeStream.ReadAsync(Memory{byte}, CancellationToken)"/>
        public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancelToken = default) {
            return _clientStream.ReadAsync(buffer, cancelToken);
        }
    }
}
