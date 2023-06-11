using System.Buffers;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace KdSoft.NamedPipe
{
    public abstract class NamedPipeMessageBase
    {
        protected readonly string _name;
        protected readonly Pipe _pipeline;

        protected readonly static ImmutableArray<byte> _messageSeparator = ImmutableArray<byte>.Empty.Add(0);

        public NamedPipeMessageBase(string name)
        {
            this._name = name;
            _pipeline = new Pipe();
        }

        /// <summary>
        /// Helper method to parse individual messages out of the buffer.
        /// </summary>
        /// <param name="buffer">Buffer to parse, gets updated when a complete message is retrieved.</param>
        /// <param name="msgBytes">The message retrieved.</param>
        /// <returns><c>true</c> if a new complete message could be parsed, <c>false</c> otherwise.</returns>
        bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> msgBytes)
        {
            var pos = buffer.PositionOf((byte)0);
            if (pos == null)
            {
                msgBytes = default;
                return false;
            }

            msgBytes = buffer.Slice(0, pos.Value);

            var nextStart = buffer.GetPosition(1, pos.Value);
            buffer = buffer.Slice(nextStart);
            return true;
        }

        /// <summary>
        /// Async enumerable returning messages received.
        /// </summary>
        /// <param name="readCancelToken">CancellationToken to cancel reading messages.</param>
        /// <param name="lastTask">Task com wait for at then end of the enumeration. Can be used for anything.</param>
        protected async IAsyncEnumerable<ReadOnlySequence<byte>> GetMessages([EnumeratorCancellation] CancellationToken readCancelToken, Task? lastTask = null)
        {
            var pipelineReader = _pipeline.Reader;
            while (!readCancelToken.IsCancellationRequested)
            {
                ReadOnlySequence<byte> buffer;
                ReadResult readResult;
                try
                {
                    readResult = await pipelineReader.ReadAsync(readCancelToken).ConfigureAwait(false);
                    buffer = readResult.Buffer;
                }
                catch (OperationCanceledException)
                {
                    pipelineReader.Complete();
                    break;
                }
                catch (Exception ex)
                {
                    pipelineReader.Complete(ex);
                    throw;
                }

                while (TryReadMessage(ref buffer, out var msgBytes))
                {
                    yield return msgBytes;
                }

                pipelineReader.AdvanceTo(buffer.Start, buffer.End);
                if (readResult.IsCompleted)
                    break;
            }
            pipelineReader.Complete();

            if (lastTask is not null)
                await lastTask.ConfigureAwait(false);
        }
    }
}
