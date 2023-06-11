using System.Buffers;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using PipeLines = System.IO.Pipelines;

namespace KdSoft.NamedPipe
{
    public class NamedPipeMessageClient: NamedPipeMessageBase, IDisposable, IAsyncDisposable
    {
        readonly NamedPipeClientStream _clientStream;
        readonly int _minBufferSize;

        NamedPipeMessageClient(string server, string name, int minBufferSize) : base(name) {
            this._minBufferSize = minBufferSize;
            var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
            _clientStream = new NamedPipeClientStream(server, _name, PipeDirection.InOut, pipeOptions);
        }

        public static async Task<NamedPipeMessageClient> ConnectAsync(string server, string name, int minBufferSize = 512) {
            var result = new NamedPipeMessageClient(server, name, minBufferSize);
            try {
                await result._clientStream.ConnectAsync().ConfigureAwait(false);
            }
            catch {
                result.Dispose();
                throw;
            }
            return result;
        }

        public void WaitForPipeDrain() {
            _clientStream.WaitForPipeDrain();
        }

        public void Reset() {
            _pipeline.Reset();
        }

        public void Dispose() {
            _clientStream.Dispose();
        }

        public ValueTask DisposeAsync() {
            return _clientStream.DisposeAsync();
        }

        async Task Listen(PipeLines.PipeWriter pipelineWriter, CancellationToken cancelToken) {
            while (!cancelToken.IsCancellationRequested) {
                PipeLines.FlushResult flr;
                try {
                    //var buffer = new byte[_minBufferSize];
                    //var count = await pipeServer.ReadAsync(buffer, _listenCancelToken).ConfigureAwait(false);
                    var memory = pipelineWriter.GetMemory(_minBufferSize);
                    var count = await _clientStream.ReadAsync(memory, cancelToken).ConfigureAwait(false);
                    if (count == 0) {
                        //flr = await pipelineWriter.FlushAsync(_cancelSource.Token).ConfigureAwait(false);
                        pipelineWriter.Complete();
                        return;
                    }

                    pipelineWriter.Advance(count);
                    //writer.Write(new Span<byte>(buffer, 0, count));

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
        public IAsyncEnumerable<ReadOnlySequence<byte>> Messages([EnumeratorCancellation] CancellationToken readCancelToken) {
            _clientStream.ReadMode = PipeTransmissionMode.Message;
            var listenTask = Listen(_pipeline.Writer, readCancelToken);
            return base.GetMessages(readCancelToken, listenTask);
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> message, bool flush = false, CancellationToken cancelToken = default) {
            var writeStreamTask = _clientStream.WriteAsync(message, cancelToken);
            if (flush) {
                await writeStreamTask.ConfigureAwait(false);
                await _clientStream.FlushAsync(cancelToken).ConfigureAwait(false);
            }
            else {
                await writeStreamTask.ConfigureAwait(false);
            }
        }

        public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancelToken = default) {
            return _clientStream.ReadAsync(buffer, cancelToken);
        }
    }
}
