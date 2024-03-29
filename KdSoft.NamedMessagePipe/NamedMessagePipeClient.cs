﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using System.Threading;
using System.Threading.Tasks;
using PipeLines = System.IO.Pipelines;

namespace KdSoft.NamedMessagePipe
{
    /// <summary>
    /// NamedPipe client that handles message buffers without null bytes (e.g. UTF8-encoded strings).
    /// To be recognized as a message, a null byte must be added to the end of the message buffer.
    /// </summary>
    public class NamedMessagePipeClient
#if NETFRAMEWORK
        : NamedMessagePipeBase, IDisposable
#else
        : NamedMessagePipeBase, IDisposable, IAsyncDisposable
#endif
    {
        readonly string _server;
        readonly int _minBufferSize;

        /// <summary>Internal instance of <see cref="NamedPipeClientStream"/>.</summary>
        protected NamedPipeClientStream _clientStream;

#if NETFRAMEWORK

        NamedMessagePipeClient(
            string server,
            string pipeName,
            string instanceId,
            TokenImpersonationLevel impersonationLevel,
            int minBufferSize
        ) : base(pipeName, instanceId) {
            this._server = server;
            this._minBufferSize = minBufferSize;
            var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
            _clientStream = new NamedPipeClientStream(server, PipeName, PipeDirection.InOut, pipeOptions, impersonationLevel);
        }

        /// <summary>
        /// Returns new instance of a connected <see cref="NamedMessagePipeClient"/>.
        /// </summary>
        /// <param name="server">Name of server. "." for local server.</param>
        /// <param name="pipeName">Name of pipe.</param>
        /// <param name="instanceId">Unique identifier of this instance.</param>
        /// <param name="timeout">Timeout for connection attempt.</param>
        /// <param name="cancelToken">Cancellation token.</param>
        /// <param name="impersonationLevel">Security impersonation level.</param>
        /// <param name="minBufferSize">Minimum buffer size to use for reading messages.</param>
        /// <returns>Connected <see cref="NamedMessagePipeClient"/> instance.</returns>
        public static async Task<NamedMessagePipeClient> ConnectAsync(
            string server,
            string pipeName,
            string instanceId,
            int timeout = Timeout.Infinite,
            CancellationToken cancelToken = default,
            TokenImpersonationLevel impersonationLevel = TokenImpersonationLevel.None,
            int minBufferSize = 512
        ) {
            var result = new NamedMessagePipeClient(server, pipeName, instanceId, impersonationLevel, minBufferSize);
            try {
                await MakeCancellable(() => result._clientStream.Connect(timeout), cancelToken).ConfigureAwait(false);
                NamedPipeEventSource.Log.ClientConnected(result.PipeName, result.InstanceId);
                return result;
            }
            catch (Exception ex) {
                NamedPipeEventSource.Log.ClientConnectionError(result.PipeName, result.InstanceId, ex);
                result.Dispose();
                throw;
            }
        }

#else

        NamedMessagePipeClient(
            string server,
            string pipeName,
            string instanceId,
            bool currentUserOnly,
            TokenImpersonationLevel impersonationLevel,
            int minBufferSize
        ) : base(pipeName, instanceId) {
            this._server = server;
            this._minBufferSize = minBufferSize;
            var pipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;
            if (currentUserOnly) {
                pipeOptions |= PipeOptions.CurrentUserOnly;
            }
            _clientStream = new NamedPipeClientStream(server, PipeName, PipeDirection.InOut, pipeOptions, impersonationLevel);
        }

        /// <summary>
        /// Returns new instance of a connected <see cref="NamedMessagePipeClient"/>.
        /// </summary>
        /// <param name="server">Name of server. "." for local server.</param>
        /// <param name="pipeName">Name of pipe.</param>
        /// <param name="instanceId">Unique identifier of this instance.</param>
        /// <param name="timeout">Timeout for connection attempt.</param>
        /// <param name="cancelToken">Cancellation token.</param>
        /// <param name="currentUserOnly">Can only connect to a server created by the same user.</param>
        /// <param name="impersonationLevel">Security impersonation level.</param>
        /// <param name="minBufferSize">Minimum buffer size to use for reading messages.</param>
        /// <returns>Connected <see cref="NamedMessagePipeClient"/> instance.</returns>
        public static async Task<NamedMessagePipeClient> ConnectAsync(
            string server,
            string pipeName,
            string instanceId,
            int timeout = Timeout.Infinite,
            CancellationToken cancelToken = default,
            bool currentUserOnly = false,
            TokenImpersonationLevel impersonationLevel = TokenImpersonationLevel.None,
            int minBufferSize = 512
        ) {
            var result = new NamedMessagePipeClient(server, pipeName, instanceId, currentUserOnly, impersonationLevel, minBufferSize);
            try {
                await result._clientStream.ConnectAsync(timeout, cancelToken).ConfigureAwait(false);
                NamedPipeEventSource.Log.ClientConnected(result.PipeName, result.InstanceId);
                return result;
            }
            catch (Exception ex) {
                NamedPipeEventSource.Log.ClientConnectionError(result.PipeName, result.InstanceId, ex);
                result.Dispose();
                throw;
            }
        }

#endif

        /// <summary> Wrapped <see cref="NamedPipeClientStream"/>.</summary>
        public NamedPipeClientStream Stream => _clientStream;

        /// <summary>
        /// Resets the <see cref="PipeLines.Pipe"/> so that the client can retart reading/listening again
        /// after having processed one or more messages from the server, therefore ending the read loop.
        /// See <see cref="PipeLines.Pipe.Reset"/>
        /// </summary>
        public void Reset() {
            _pipeline.Reset();
        }

        /// <summary>Implementation of Dispose pattern.</summary>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                _clientStream.Dispose();
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
        /// <param name="readCancelToken">Cancellation token that ends the async enumeration.</param>
        /// <remarks>
        /// When cancelled, and before the next call to <see cref="Messages(CancellationToken)"/>,
        /// it is ncessary to call <see cref="Reset"/>.
        /// </remarks>
        public IAsyncEnumerable<ReadOnlySequence<byte>> Messages(CancellationToken readCancelToken = default) {
            // we need to cancel the Listen() loop in two cases:
            // 1) the readCancelToken is triggered
            // 2) the read loop (base.GetMessages) terminates
            var listenCancelSource = new CancellationTokenSource();
            var messagesCancelSource = CancellationTokenSource.CreateLinkedTokenSource(readCancelToken, listenCancelSource.Token);
            var listenTask = Listen(_pipeline.Writer, messagesCancelSource.Token);

#if NETFRAMEWORK
            // In the full framework cancellation does not work, because read operations cannot be cancelled once started.
            // So we need to dispose (and re-create) the client in order to stop the read loop.
            messagesCancelSource.Token.Register(() => {
                Dispose();
            });
#endif

            async Task LastStep() {
                // we do this to cancel/stop the listen loop
                listenCancelSource.Cancel();
                listenCancelSource.Dispose();
                messagesCancelSource.Dispose();
                await listenTask.ConfigureAwait(false);
            }
            return base.GetMessages(CancellationToken.None, LastStep);
        }

        async Task Listen(PipeLines.PipeWriter pipelineWriter, CancellationToken cancelToken) {
#if NETFRAMEWORK
            var buffer = new byte[_minBufferSize];
#endif
            while (!cancelToken.IsCancellationRequested) {
                PipeLines.FlushResult writeResult = default;
                try {
#if NETFRAMEWORK
                    //NOTE for .NET Framework: the listener loop will advance to the next Read before the cancelToken is triggered,
                    //     waiting there forever, because in .NET framework we cant cancel the read properly once it has started;
                    //     seems we need to dispose and re-create the client in order to stop the read loop while waiting for data.

                    NamedPipeEventSource.Log.ListenBeginRead(nameof(NamedMessagePipeClient), PipeName, InstanceId);
                    var byteCount = await MakeCancellable(() => _clientStream.Read(buffer, 0, buffer.Length), cancelToken).ConfigureAwait(false);
                    NamedPipeEventSource.Log.ListenEndRead(nameof(NamedMessagePipeClient), PipeName, InstanceId);
                    var memory = new ReadOnlyMemory<byte>(buffer, 0, byteCount);
                    writeResult = await pipelineWriter.WriteAsync(memory, cancelToken).ConfigureAwait(false);
                    if (writeResult.IsCompleted || writeResult.IsCanceled) {
                        break;
                    }
#else
                    var memory = pipelineWriter.GetMemory(_minBufferSize);
                    NamedPipeEventSource.Log.ListenBeginRead(nameof(NamedMessagePipeClient), PipeName, InstanceId);
                    var byteCount = await _clientStream.ReadAsync(memory, cancelToken).ConfigureAwait(false);
                    NamedPipeEventSource.Log.ListenEndRead(nameof(NamedMessagePipeClient), PipeName, InstanceId);
                    pipelineWriter.Advance(byteCount);
#endif

                    if (byteCount == 0) {
                        break;
                    }

                    writeResult = await pipelineWriter.FlushAsync(cancelToken).ConfigureAwait(false);
                    if (writeResult.IsCompleted || writeResult.IsCanceled) {
                        break;
                    }
                }
                catch (OperationCanceledException) {
                    NamedPipeEventSource.Log.ListenCancel(nameof(NamedMessagePipeClient), PipeName, InstanceId);
                    break;
                }
                catch (IOException ioex) {
                    NamedPipeEventSource.Log.ClientConnectionError(PipeName, InstanceId, ioex);
                    break;
                }
                catch (Exception ex) {
                    NamedPipeEventSource.Log.ListenError(nameof(NamedMessagePipeClient), PipeName, InstanceId, ex);
                    await pipelineWriter.CompleteAsync(ex).ConfigureAwait(false);
                    throw;
                }
            }

            NamedPipeEventSource.Log.ListenEnd(nameof(NamedMessagePipeClient), PipeName, InstanceId);
            await pipelineWriter.CompleteAsync().ConfigureAwait(false);
        }

#if NETFRAMEWORK
        /// <inheritdoc cref="Stream.WriteAsync(byte[], int, int, CancellationToken)"/>
        public Task WriteAsync(byte[] message, int offset, int count, CancellationToken cancelToken = default) {
            return MakeCancellable(() => _clientStream.Write(message, offset, count), cancelToken);
        }

        /// <inheritdoc cref="Stream.ReadAsync(byte[], int, int, CancellationToken)"/>
        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken = default) {
            return MakeCancellable(() => _clientStream.Read(buffer, offset, count), cancelToken);
        }

        /// <inheritdoc cref="Stream.FlushAsync(CancellationToken)"/>
        public Task FlushAsync(CancellationToken cancelToken = default) {
            return MakeCancellable(() => _clientStream.Flush(), cancelToken);
        }
#else
        /// <inheritdoc />
        public ValueTask DisposeAsync() {
            return _clientStream.DisposeAsync();
        }

#if NETSTANDARD2_1
        /// <inheritdoc cref="Stream.FlushAsync(CancellationToken)"/>
        public Task FlushAsync(CancellationToken cancelToken = default) {
            return MakeCancellable(() => _clientStream.Flush(), cancelToken);
        }
#endif

#endif
    }
}
