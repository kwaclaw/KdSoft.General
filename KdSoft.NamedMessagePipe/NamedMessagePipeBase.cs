using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.NamedMessagePipe
{
    /// <summary>
    /// Base class for <see cref="NamedMessagePipeServer"/> and <see cref="NamedMessagePipeClient"/>.
    /// </summary>
    public abstract class NamedMessagePipeBase
    {
        readonly string _instanceId;
        readonly string _pipeName;

        /// <summary><see cref="Pipe"/> used internally for message processing.</summary>
        protected readonly Pipe _pipeline;
        /// <summary>Message separator to pass to <see cref="BuffersExtensions.Write{T}(IBufferWriter{T}, ReadOnlySpan{T})"/>.</summary>
        protected readonly static ReadOnlyMemory<byte> _messageSeparator = new ReadOnlyMemory<byte>(new byte[] { 0 });

        /// <summary>Constructor.</summary>
        /// <param name="name">Name of pipe.</param>
        /// <param name="instanceId">Identifier of the new instance.</param>
        public NamedMessagePipeBase(string name, string instanceId) {
            this._pipeName = name;
            this._instanceId = instanceId;
            _pipeline = new Pipe();
        }

        /// <summary>Unique identifier of this instance.</summary>
        public string InstanceId => _instanceId;

        /// <summary>Name of pipe.</summary>
        public string PipeName => _pipeName;

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
        /// <param name="lastStep">Operation returning a Task that will be awaited at then end of the enumeration. This ensures
        /// that the enumeration finishes *after* the task has completed. Can be used for anything.</param>
        protected async IAsyncEnumerable<ReadOnlySequence<byte>> GetMessages([EnumeratorCancellation] CancellationToken readCancelToken, Func<Task>? lastStep = null) {
            var pipelineReader = _pipeline.Reader;
            ReadResult readResult = default;
            try {
                while (!readCancelToken.IsCancellationRequested) {
                    ReadOnlySequence<byte> buffer;
                    try {
                        NamedPipeEventSource.Log.GetMessagesBeginRead(GetType().Name, PipeName, InstanceId);
                        readResult = await pipelineReader.ReadAsync(readCancelToken).ConfigureAwait(false);
                        buffer = readResult.Buffer;
                        NamedPipeEventSource.Log.GetMessagesEndRead(GetType().Name, PipeName, InstanceId);
                    }
                    catch (OperationCanceledException) {
                        NamedPipeEventSource.Log.GetMessagesCancel(GetType().Name, PipeName, InstanceId);
                        break;
                    }
                    catch (Exception ex) {
                        NamedPipeEventSource.Log.GetMessagesError(GetType().Name, PipeName, InstanceId, ex);
                        await pipelineReader.CompleteAsync(ex).ConfigureAwait(false);
                        throw;
                    }

                    while (TryReadMessage(ref buffer, out var msgBytes)) {
                        yield return msgBytes;
                    }

                    if (readResult.IsCompleted || readResult.IsCanceled) {
                        break;
                    }

                    pipelineReader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            finally {
                NamedPipeEventSource.Log.GetMessagesEnd(GetType().Name, PipeName, InstanceId, !(readResult.IsCompleted || readResult.IsCanceled));
                if (lastStep is not null)
                    await lastStep().ConfigureAwait(false);
                await pipelineReader.CompleteAsync().ConfigureAwait(false);
            }
        }

#if NETFRAMEWORK
        /// <summary>
        /// Helper for .NET framework to make methods cancellable where cancelling
        /// does not work, e.g. Stream.ReadAsync() or Stream.WriteAsync().
        /// NOTE: this DOES NOT perform a real cancellation of the operation,
        /// the operation's Task will simply be abandoned, so its effects may linger on.
        /// </summary>
        public static async Task MakeCancellable(Task task, CancellationToken cancelToken) {
            var observeExceptionTask = task.ContinueWith(t => {
                var ex = task.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);

            var cancelSource = new TaskCompletionSource<object>();
            cancelToken.Register(() => {
                cancelSource.SetCanceled();
            });
            var resultTask = await Task.WhenAny(task, cancelSource.Task).ConfigureAwait(false);
            await resultTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Helper for .NET framework to make methods cancellable where cancelling
        /// does not work, e.g. Stream.ReadAsync() or Stream.WriteAsync().
        /// NOTE: this DOES NOT perform a real cancellation of the operation,
        /// the operation's Task will simply be abandoned, so its effects may linger on.
        /// </summary>
        public static async Task MakeCancellable(Action action, CancellationToken cancelToken) {
            var task = new Task(action);
            var observeExceptionTask = task.ContinueWith(t => {
                var ex = task.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);

            var cancelSource = new TaskCompletionSource<object>();
            cancelToken.Register(() => {
                cancelSource.SetCanceled();
            });

            task.Start();
            var resultTask = await Task.WhenAny(task, cancelSource.Task).ConfigureAwait(false);
            await resultTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Helper for .NET framework to make methods cancellable where cancelling
        /// does not work, e.g. Stream.ReadAsync() or Stream.WriteAsync().
        /// NOTE: this DOES NOT perform a real cancellation of the operation,
        /// the operation's Task will simply be abandoned, so its effects may linger on.
        /// </summary>
        public static async Task<T> MakeCancellable<T>(Task<T> task, CancellationToken cancelToken) {
            var observeExceptionTask = task.ContinueWith(t => {
                var ex = task.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
            
            var cancelSource = new TaskCompletionSource<T>();
            cancelToken.Register(() => {
                cancelSource.SetCanceled();
            });
            var resultTask = await Task.WhenAny<T>(task, cancelSource.Task).ConfigureAwait(false);
            return await resultTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Helper for .NET framework to make methods cancellable where cancelling
        /// does not work, e.g. Stream.ReadAsync() or Stream.WriteAsync().
        /// NOTE: this DOES NOT perform a real cancellation of the operation,
        /// the operation's Task will simply be abandoned, so its effects may linger on.
        /// </summary>
        public static async Task<T> MakeCancellable<T>(Func<T> func, CancellationToken cancelToken) {
            var task = new Task<T>(func);
            var observeExceptionTask = task.ContinueWith(t => {
                var ex = task.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);

            var cancelSource = new TaskCompletionSource<T>();
            cancelToken.Register(() => {
                cancelSource.SetCanceled();
            });

            task.Start();
            var resultTask = await Task.WhenAny<T>(task, cancelSource.Task).ConfigureAwait(false);
            return await resultTask.ConfigureAwait(false);
        }
#endif
    }
}
