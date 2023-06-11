using System.Buffers;
using System.IO.Pipes;
using PipeLines = System.IO.Pipelines;

#if false

namespace KdSoft.NamedPipe
{
    public class NamedPipeMessageClient_Old: NamedPipeMessageBase, IDisposable, IAsyncDisposable
    {
        readonly NamedPipeClientStream _clientStream;
        readonly CancellationTokenSource _cancelSource;
        readonly int _minBufferSize;

        NamedPipeMessageClient_Old(string server, string name, int minBufferSize, CancellationTokenSource cancelSource) : base(name, cancelSource.Token) {
            this._minBufferSize = minBufferSize;
            this._cancelSource = cancelSource;
            var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
            _clientStream = new NamedPipeClientStream(server, _name, PipeDirection.InOut, pipeOptions);
        }

        public static async Task<NamedPipeMessageClient_Old> ConnectAsync(string server, string name, int minBufferSize = 512) {
            var listenCancelSource = new CancellationTokenSource();
            var result = new NamedPipeMessageClient_Old(server, name, minBufferSize, listenCancelSource);
            try {
                await result._clientStream.ConnectAsync().ConfigureAwait(false);
                result.StartListening();
            }
            catch {
                result.Dispose();
                throw;
            }
            return result;
        }

        /// <summary>
        /// Listener Task that can be awaited.
        /// </summary>
        public Task ListenTask { get; private set; }

        public void WaitForPipeDrain() {
            _clientStream.WaitForPipeDrain();
        }

        public void Dispose() {
            _cancelSource.Cancel();
            _clientStream.Dispose();
        }

        public ValueTask DisposeAsync() {
            _cancelSource.Cancel();
            return _clientStream.DisposeAsync();
        }

        void StartListening() {
            _clientStream.ReadMode = PipeTransmissionMode.Message;
            ListenTask = Listen(_pipeline.Writer);
        }

        async Task Listen(PipeLines.PipeWriter pipelineWriter) {
            while (!_cancelSource.Token.IsCancellationRequested) {
                PipeLines.FlushResult flr;

                //var buffer = new byte[_minBufferSize];
                //var count = await pipeServer.ReadAsync(buffer, _listenCancelToken).ConfigureAwait(false);
                var memory = pipelineWriter.GetMemory(_minBufferSize);
                var count = await _clientStream.ReadAsync(memory, _cancelSource.Token).ConfigureAwait(false);
                if (count == 0) {
                    //flr = await pipelineWriter.FlushAsync(_cancelSource.Token).ConfigureAwait(false);
                    return;
                }

                pipelineWriter.Advance(count);
                //writer.Write(new Span<byte>(buffer, 0, count));

                if (_clientStream.IsMessageComplete) {
                    // we assume UTF8 string data, so we can use 0 as message separator
                    pipelineWriter.Write(_messageSeparator.AsSpan());
                }

                flr = await pipelineWriter.FlushAsync(_cancelSource.Token);
                if (flr.IsCompleted) {
                    return;
                }
            }
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> message, bool flush = false, CancellationToken cancelToken = default) {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _listenCancelToken);
            var writeStreamTask = _clientStream.WriteAsync(message, cts.Token);
            if (flush) {
                await writeStreamTask.ConfigureAwait(false);
                await _clientStream.FlushAsync(cts.Token).ConfigureAwait(false);
            }
            else {
                await writeStreamTask.ConfigureAwait(false);
            }
        }

        public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancelToken = default) {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _listenCancelToken);
            return _clientStream.ReadAsync(buffer, cts.Token);
        }
    }
}
#endif