using System.Buffers;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace KdSoft.NamedMessagePipe
{
    /// <summary>
    /// Base class for <see cref="NamedMessagePipeServer"/> and <see cref="NamedMessagePipeClient"/>.
    /// </summary>
    public abstract class NamedMessagePipeBase
    {
        /// <summary>Name of pipe.</summary>
        protected readonly string _name;
        /// <summary><see cref="Pipe"/> used internally for message processing.</summary>
        protected readonly Pipe _pipeline;
        /// <summary>Message separator to pass to <see cref="BuffersExtensions.Write{T}(IBufferWriter{T}, ReadOnlySpan{T})"/>.</summary>
        protected readonly static ImmutableArray<byte> _messageSeparator = ImmutableArray<byte>.Empty.Add(0);

        /// <summary>Constructor.</summary>
        /// <param name="name">Name of pipe.</param>
        public NamedMessagePipeBase(string name) {
            this._name = name;
            _pipeline = new Pipe();
        }

        /// <summary>
        /// Helper method to parse individual messages out of the buffer.
        /// </summary>
        /// <param name="buffer">Buffer to parse, gets updated when a complete message is retrieved.</param>
        /// <param name="msgBytes">The message retrieved.</param>
        /// <returns><c>true</c> if a new complete message could be parsed, <c>false</c> otherwise.</returns>
        bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> msgBytes) {
            var pos = buffer.PositionOf((byte)0);
            if (pos == null) {
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
        /// <param name="lastTask">Task to wait for at then end of the enumeration. This ensures
        /// that the enumeration finishes *after* the task has completed. Can be used for anything.</param>
        protected async IAsyncEnumerable<ReadOnlySequence<byte>> GetMessages([EnumeratorCancellation] CancellationToken readCancelToken, Task? lastTask = null) {
            var pipelineReader = _pipeline.Reader;
            while (!readCancelToken.IsCancellationRequested) {
                ReadOnlySequence<byte> buffer;
                ReadResult readResult;
                try {
                    readResult = await pipelineReader.ReadAsync(readCancelToken).ConfigureAwait(false);
                    buffer = readResult.Buffer;
                }
                catch (OperationCanceledException) {
                    pipelineReader.Complete();
                    break;
                }
                catch (Exception ex) {
                    pipelineReader.Complete(ex);
                    throw;
                }

                while (TryReadMessage(ref buffer, out var msgBytes)) {
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
