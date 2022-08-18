using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace miniBBS.Services.Services
{
    public class Xmodem : IFileTransferProtocol
    {
        private int _offset = 0;
        private byte _packetNum = 0;
        private const int _timeoutSec = 30;
        private const int _maxErrors = 10;

        #region ByteCodes
        /// <summary>
        /// Start of Header
        /// </summary>
        private const byte SOH = 0x01;

        /// <summary>
        /// End of Transmission
        /// </summary>
        private const byte EOT = 0x04;

        /// <summary>
        /// Achnowledge
        /// </summary>
        private const byte ACK = 0x06;

        /// <summary>
        /// Not Achnowledge
        /// </summary>
        private const byte NAK = 0x15;

        /// <summary>
        /// End of Transmission Block
        /// </summary>
        private const byte ETB = 0x17;

        /// <summary>
        /// Cancel
        /// </summary>
        private const byte CAN = 0x18;

        /// <summary>
        /// ASCII 'C'
        /// </summary>
        private const byte C = 0x43;

        /// <summary>
        /// Data padding (^Z)
        /// </summary>
        private const byte PAD = 0x1a;
        #endregion

        private List<byte> _data = new List<byte>();
        /// <summary>
        /// The data to be sent when calling Send or the buffer to place the data when calling Receive
        /// </summary>
        public byte[] Data
        {
            get
            {
                return _data.ToArray();
            }
            set
            {
                _data = value == null ? new List<byte>() : new List<byte>(value);
            }
        }

        /// <summary>
        /// Sends the Data and returns true if all data was sent successfully.
        /// </summary>
        public bool Send(BbsSession session)
        {
            var sendStarted = false;
            var waitToStartStopwatch = Stopwatch.StartNew();
            var waitForNextRequestStopwatch = new Stopwatch();
            var canceled = false;
            byte[] lastPacket = null;
            var errorCount = 0;
            bool use16bitChecksum = false;
            var data = Data;

            while (!canceled && session.Stream.CanRead && session.Stream.CanWrite && (sendStarted || waitToStartStopwatch.Elapsed.TotalSeconds < _timeoutSec))
            {
                var inputBuffer = new byte[1] { 0 };
                var readResult = session.Stream.BeginRead(inputBuffer, 0, 1, null, null);
                waitForNextRequestStopwatch.Start();
                while (!readResult.IsCompleted)
                {
                    Thread.Sleep(25);
                    if (waitForNextRequestStopwatch.Elapsed.TotalSeconds > 10)
                    {
                        canceled = true;                        
                        break;
                    }
                }
                var req = inputBuffer[0];
                waitForNextRequestStopwatch.Stop();

                if (!sendStarted && (C.Equals(req) || NAK.Equals(req)))
                {
                    use16bitChecksum = C.Equals(req);
                    sendStarted = true;
                    waitToStartStopwatch.Stop();
                    lastPacket = GetNextPacketToSend(data, use16bitChecksum);
                    session.Io.OutputRaw(lastPacket);
                }
                else
                {
                    switch (req)
                    {
                        case C:
                        case ACK:
                            lastPacket = GetNextPacketToSend(data, use16bitChecksum);
                            session.Io.OutputRaw(lastPacket);
                            break;
                        case NAK:
                            if (++errorCount > _maxErrors)
                                canceled = true;
                            else
                                session.Io.OutputRaw(lastPacket);
                            break;
                        case CAN: 
                            canceled = true; 
                            break;
                        case EOT:
                            canceled = true;
                            break;
                    }
                }
                Thread.Sleep(25);
            }

            return _offset >= Data.Length - 1;
        }

        /// <summary>
        /// Receives the Data and returns true if all data was received successfully.
        /// </summary>
        public bool Receive(BbsSession session)
        {
            _offset = 0;
            _data = new List<byte>();

            bool transferStarted = false;
            bool completed = false;
            bool completedSuccesfully = false;
            int errorCount = 0;
            int lastPacketNum = 0;
            int startRetries = 10;

            Stopwatch sw = new Stopwatch();

            do
            {
                byte[] inputBuffer = new byte[133];
                                
                if (!transferStarted)
                {
                    // keep sending 'C' until we get a packet
                    session.Io.OutputRaw(C);
                    var readResult = session.Stream.BeginRead(inputBuffer, 0, inputBuffer.Length, null, null);
                    sw.Reset();
                    sw.Start();
                    while (!readResult.IsCompleted)
                    {
                        Thread.Sleep(25);
                        if (sw.Elapsed.TotalSeconds > 10)
                        {
                            if (--startRetries <= 0)
                            {
                                completed = true;
                                completedSuccesfully = false;
                            }
                            break;
                        }
                    }
                    sw.Stop();
                }
                else
                {
                    var readResult = session.Stream.BeginRead(inputBuffer, 0, inputBuffer.Length, null, null);
                    sw.Reset();
                    sw.Start();
                    while (!readResult.IsCompleted)
                    {
                        Thread.Sleep(25);
                        if (sw.Elapsed.TotalSeconds > 10)
                        {
                            if (++errorCount > _maxErrors)
                            {
                                completed = true;
                                completedSuccesfully = false;
                            }
                            else
                                session.Io.OutputRaw(NAK);
                            break;
                        }
                    }
                    sw.Stop();
                }

                // while packet doesn't start with EOT or CAN ...
                var header = inputBuffer[0];
                switch (header)
                {
                    case SOH:
                        transferStarted = true;
                        if (ProcessIncomingPacket(inputBuffer, ref lastPacketNum))
                            session.Io.OutputRaw(ACK);
                        else if (++errorCount > _maxErrors)
                        {
                            completed = true;
                            completedSuccesfully = false;
                        }
                        else
                            session.Io.OutputRaw(NAK);
                        break;
                    case EOT:
                    case ETB:
                        completed = true;
                        completedSuccesfully = true;
                        break;
                    case CAN:
                        completed = true;
                        completedSuccesfully = false;
                        break;
                }

            } while (!completed);

            return completedSuccesfully;
        }

        private bool ProcessIncomingPacket(byte[] inputBuffer, ref int lastPacketNum)
        {
            // get packet number
            int packetNumber = inputBuffer[1];

            // verify packet number
            int verifyPacketNumber = inputBuffer[2];
            if ((verifyPacketNumber | packetNumber) != 255)
                return false;

            // verify packet number is lastPacketNum + 1
            if (packetNumber != lastPacketNum + 1)
                return false;

            // extract payload
            var payload = inputBuffer.Skip(3).Take(128).ToArray();

            // calculate checksum on the payload
            var crc = GetCrc16(payload);

            // verify that calculated checksum matches the checksum in the packet
            if (crc[0] != inputBuffer[131] || crc[1] != inputBuffer[132])
                return false;
            else
                _data.AddRange(payload);

            // increment lastPacketNum
            lastPacketNum = packetNumber;

            return true;
        }

        private byte[] GetNextPacketToSend(byte[] data, bool use16bitChecksum)
        {
            if (_offset >= data.Length)
                return new[] { EOT };

            var packet = new byte[use16bitChecksum ? 133 : 132];
            packet[0] = SOH;

            // packet numbers start at 1 but wrap around to 0 so increment before using
            _packetNum = (byte)((_packetNum + 1) % 256);
            packet[1] = _packetNum;
            packet[2] = (byte)~_packetNum;
            var payload = new byte[128];
            for (int i = 0; i < 128; i++)
            {
                byte b = _offset >= data.Length ? PAD : data[_offset++];
                payload[i] = b;
                packet[3 + i] = b;
            }

            if (use16bitChecksum)
            {
                var crc = GetCrc16(payload);
                packet[131] = crc[0];
                packet[132] = crc[1];
            }
            else
            {
                packet[131] = GetCrc8(payload);
            }

            return packet;
        }

        private static byte[] GetCrc16(byte[] data)
        {
            int c = CalculateCrc16(data);
            byte low = (byte)(c % 256);
            byte high = (byte)((c - low) >> 8);
            var crc = new byte[] { high, low };
            return crc;
        }

        // Adapted from sample C code found at https://web.mit.edu/6.115/www/amulet/xmodem.htm
        private static int CalculateCrc16(byte[] data)
        {
            int crc = 0;
            int count = data.Length;
            int ptr = 0;
            int i;

            while (--count >= 0)
            {
                crc ^= (data[ptr++] << 8);
                i = 8;
                do
                {
                    if ((crc & 0x8000) == 0x8000)
                        crc <<= 1 ^ 0x1021;
                    else
                        crc <<= 1;
                } while (--i > 0);
            }

            return crc;
        }

        private static byte GetCrc8(byte[] data)
        {
            return (byte)(data.Sum(b => b) % 256);
        }
    }
}
