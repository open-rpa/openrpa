using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using OpenRPA.NamedPipeWrapper.IO;
using OpenRPA.NamedPipeWrapper.Threading;

namespace OpenRPA.NamedPipeWrapper
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeClientStream"/>.
    /// </summary>
    /// <typeparam name="TReadWrite">Reference type to read from and write to the named pipe</typeparam>
    public class NamedPipeClient<TReadWrite> : NamedPipeClient<TReadWrite, TReadWrite> where TReadWrite : class
    {
        /// <summary>
        /// Constructs a new <c>NamedPipeClient</c> to connect to the NamedPipeNamedPipeServer{TReadWrite} specified by <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        /// <param name="serverName">server name default is local.</param>
        public NamedPipeClient(string pipeName,string serverName=".") : base(pipeName, serverName)
        {
        }
    }

    /// <summary>
    /// Wraps a <see cref="NamedPipeClientStream"/>.
    /// </summary>
    /// <typeparam name="TRead">Reference type to read from the named pipe</typeparam>
    /// <typeparam name="TWrite">Reference type to write to the named pipe</typeparam>
    public class NamedPipeClient<TRead, TWrite>
        where TRead : class
        where TWrite : class
    {
        /// <summary>
        /// Gets or sets whether the client should attempt to reconnect when the pipe breaks
        /// due to an error or the other end terminating the connection.
        /// Default value is <c>true</c>.
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// Invoked whenever a message is received from the server.
        /// </summary>
        public event ConnectionMessageEventHandler<TRead, TWrite> ServerMessage;

        /// <summary>
        /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
        /// </summary>
        public event ConnectionEventHandler<TRead, TWrite> Disconnected;
        public event ConnectionEventHandler<TRead, TWrite> Connected;
        

        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
        /// </summary>
        public event PipeExceptionEventHandler Error;

        private readonly string _pipeName;
        private NamedPipeConnection<TRead, TWrite> _connection;

        public bool isConnected
        {
            get
            {
                if (_connection == null) return false;
                return _connection.IsConnected;
            }
        }

        private readonly AutoResetEvent _connected = new AutoResetEvent(false);
        private readonly AutoResetEvent _disconnected = new AutoResetEvent(false);

        private volatile bool _closedExplicitly;
        /// <summary>
        /// the server name, which client will connect to.
        /// </summary>
        private string _serverName { get; set; }

        /// <summary>
        /// Constructs a new <c>NamedPipeClient</c> to connect to the NamedPipeServer{TRead, TWrite} specified by <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        /// <param name="serverName">the Name of the server, default is  local machine</param>
        public NamedPipeClient(string pipeName,string serverName)
        {
            _pipeName = pipeName;
            _serverName = serverName;
            AutoReconnect = true;
        }

        /// <summary>
        /// Connects to the named pipe server asynchronously.
        /// This method returns immediately, possibly before the connection has been established.
        /// </summary>
        public void Start()
        {
            _closedExplicitly = false;
            var worker = new Worker();
            worker.Error += OnError;
            worker.DoWork(ListenSync, "PIPE " + _pipeName);
        }

        /// <summary>
        ///     Sends a message to the server over a named pipe.
        /// </summary>
        /// <param name="message">Message to send to the server.</param>
        public void PushMessage(TWrite message)
        {
            if (_connection != null)
                _connection.PushMessage(message);
        }

        /// <summary>
        /// Closes the named pipe.
        /// </summary>
        public void Stop()
        {
            _closedExplicitly = true;
            if (_connection != null)
                _connection.Close();
        }

        #region Wait for connection/disconnection

        public void WaitForConnection()
        {
            _connected.WaitOne();
        }

        public void WaitForConnection(int millisecondsTimeout)
        {
            _connected.WaitOne(millisecondsTimeout);
        }

        public void WaitForConnection(TimeSpan timeout)
        {
            _connected.WaitOne(timeout);
        }

        public void WaitForDisconnection()
        {
            _disconnected.WaitOne();
        }

        public void WaitForDisconnection(int millisecondsTimeout)
        {
            _disconnected.WaitOne(millisecondsTimeout);
        }

        public void WaitForDisconnection(TimeSpan timeout)
        {
            _disconnected.WaitOne(timeout);
        }

        #endregion

        #region Private methods

        private void ListenSync()
        {
            try
            {
                // Get the name of the data pipe that should be used from now on by this NamedPipeClient
                var handshake = PipeClientFactory.Connect<string, string>(_pipeName, _serverName);
                var dataPipeName = handshake.ReadObject();
                handshake.Close();

                // Connect to the actual data pipe
                var dataPipe = PipeClientFactory.CreateAndConnectPipe(dataPipeName, _serverName);
                // Create a Connection object for the data pipe
                _connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
                Connected?.Invoke(_connection);
                _connection.Disconnected += OnDisconnected;
                _connection.ReceiveMessage += OnReceiveMessage;
                _connection.Error += ConnectionOnError;
                _connection.Open();

                _connected.Set();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString(), "Error");
                throw;
            }
        }

        private void OnDisconnected(NamedPipeConnection<TRead, TWrite> connection)
        {
            Disconnected?.Invoke(connection);

            _disconnected.Set();

            // Reconnect
            if (AutoReconnect && !_closedExplicitly)
                Start();
        }

        private void OnReceiveMessage(NamedPipeConnection<TRead, TWrite> connection, TRead message)
        {
            ServerMessage?.Invoke(connection, message);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        private void ConnectionOnError(NamedPipeConnection<TRead, TWrite> connection, Exception exception)
        {
            OnError(exception);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        /// <param name="exception"></param>
        private void OnError(Exception exception)
        {
            Error?.Invoke(exception);
        }

        #endregion
    }

    static class PipeClientFactory
    {
        public static PipeStreamWrapper<TRead, TWrite> Connect<TRead, TWrite>(string pipeName,string serverName)
            where TRead : class
            where TWrite : class
        {
            return new PipeStreamWrapper<TRead, TWrite>(CreateAndConnectPipe(pipeName,serverName));
        }

        public static NamedPipeClientStream CreateAndConnectPipe(string pipeName, string serverName)
        {
            var pipe = CreatePipe(pipeName, serverName);
            pipe.Connect();
            return pipe;
        }

        private static NamedPipeClientStream CreatePipe(string pipeName,string serverName)
        {
            return new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }
    }
}
