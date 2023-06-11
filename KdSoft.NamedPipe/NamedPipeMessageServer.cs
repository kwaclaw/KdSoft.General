using System.Buffers;
using System.IO.Pipes;
using PipeLines = System.IO.Pipelines;

namespace KdSoft.NamedPipe
{
    public class NamedPipeMessageServer: NamedPipeMessageBase, IDisposable, IAsyncDisposable
    {
        readonly NamedPipeServerStream _server;
        readonly int _minBufferSize;
        protected readonly CancellationToken _listenCancelToken;
        protected readonly Task _listenTask;

        public NamedPipeMessageServer(
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

        public Task ListenTask => _listenTask;

        public void Dispose() => _server.Dispose();
        public ValueTask DisposeAsync() => _server.DisposeAsync();

        async Task Listen(PipeLines.PipeWriter pipelineWriter) {
            while (!_listenCancelToken.IsCancellationRequested) {
                try {
                    await _server.WaitForConnectionAsync().ConfigureAwait(false);

                    while (!_listenCancelToken.IsCancellationRequested) {
                        PipeLines.FlushResult flr;

                        //var buffer = new byte[_minBufferSize];
                        //var count = await pipeServer.ReadAsync(buffer, _listenCancelToken).ConfigureAwait(false);
                        var memory = pipelineWriter.GetMemory(_minBufferSize);
                        var count = await _server.ReadAsync(memory, _listenCancelToken).ConfigureAwait(false);
                        if (count == 0) {
                            pipelineWriter.Complete();
                            break;
                        }

                        pipelineWriter.Advance(count);
                        //writer.Write(new Span<byte>(buffer, 0, count));

                        if (_server.IsMessageComplete) {
                            // we assume UTF8 string data, so we can use 0 as message separator
                            pipelineWriter.Write(_messageSeparator.AsSpan());
                        }

                        flr = await pipelineWriter.FlushAsync(_listenCancelToken).ConfigureAwait(false);
                        if (flr.IsCompleted) {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) {
                    pipelineWriter.Complete();
                    break;
                }
                catch (Exception ex) {
                    pipelineWriter.Complete(ex);
                    break;
                }
            }

            _server.Disconnect();
        }


        /// <summary>
        /// Async enumerable returning messages received.
        /// </summary>
        public IAsyncEnumerable<ReadOnlySequence<byte>> Messages() {
            return base.GetMessages(_listenCancelToken, _listenTask);
        }

        public void Disconnect() {
            _server.Disconnect();
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> message, CancellationToken cancelToken = default) {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _listenCancelToken);
            return _server.WriteAsync(message, cts.Token);
        }
    }
}
