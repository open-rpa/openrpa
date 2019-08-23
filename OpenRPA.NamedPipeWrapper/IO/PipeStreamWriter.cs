using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace OpenRPA.NamedPipeWrapper.IO
{
    /// <summary>
    /// Wraps a <see cref="PipeStream"/> object and writes to it.  Serializes .NET CLR objects specified by <typeparamref name="T"/>
    /// into binary form and sends them over the named pipe for a <see cref="PipeStreamWriter{T}"/> to read and deserialize.
    /// </summary>
    /// <typeparam name="T">Reference type to serialize</typeparam>
    public class PipeStreamWriter<T> where T : class
    {
        /// <summary>
        /// Gets the underlying <c>PipeStream</c> object.
        /// </summary>
        public PipeStream BaseStream { get; private set; }

        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();

        /// <summary>
        /// Constructs a new <c>PipeStreamWriter</c> object that writes to given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Pipe to write to</param>
        public PipeStreamWriter(PipeStream stream)
        {
            BaseStream = stream;
        }

        #region Private stream writers

        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        private byte[] Serialize(T obj)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    _binaryFormatter.Serialize(memoryStream, obj);
                    return memoryStream.ToArray();
                }
            }
            catch
            {
                //if any exception in the serialize, it will stop named pipe wrapper, so there will ignore any exception.
                return null;
            }
        }

        private void WriteLength(int len)
        {
            var lenbuf = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(len));
            BaseStream.Write(lenbuf, 0, lenbuf.Length);
        }

        private void WriteObject(byte[] data)
        {
            BaseStream.Write(data, 0, data.Length);
        }

        private void Flush()
        {
            BaseStream.Flush();
        }

        #endregion

        /// <summary>
        /// Writes an object to the pipe.  This method blocks until all data is sent.
        /// </summary>
        /// <param name="obj">Object to write to the pipe</param>
        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        public void WriteObject(T obj)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            //var data = Serialize(obj); // Time elapsed: 00:00:05
            //var data = Compress(Serialize(obj));  // Time elapsed: 00:00:06
            //var data = Compress2(Serialize(obj));  // Time elapsed: 00:00:05
            //byte[] data = System.Text.Encoding.ASCII.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
            WriteLength(data.Length);
            WriteObject(data);
            Flush();
        }
        public const int BUFFER_SIZE = 1024;
        public static byte[] Compress2(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionMode.Compress))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
        public static byte[] Compress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var compressIntoMs = new MemoryStream())
            {
                using (var gzs = new BufferedStream(new GZipStream(compressIntoMs,
                 CompressionMode.Compress), BUFFER_SIZE))
                {
                    gzs.Write(inputData, 0, inputData.Length);
                }
                return compressIntoMs.ToArray();
            }
        }


        /// <summary>
        ///     Waits for the other end of the pipe to read all sent bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The pipe is closed.</exception>
        /// <exception cref="NotSupportedException">The pipe does not support write operations.</exception>
        /// <exception cref="IOException">The pipe is broken or another I/O error occurred.</exception>
        public void WaitForPipeDrain()
        {
            BaseStream.WaitForPipeDrain();
        }
    }
}