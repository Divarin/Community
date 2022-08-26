using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        /// Start of TeXt
        /// </summary>
        private const byte STX = 0x02;

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
        private const byte PAD = 0;// 0x1a;
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

        private FileTransferProtocolOptions Options { get; set; }

        /// <summary>
        /// Sends the Data and returns true if all data was sent successfully.
        /// </summary>
        public bool Send(BbsSession session, FileTransferProtocolOptions options = FileTransferProtocolOptions.None)
        {
            Options = options;

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
                var inputBuffer = new byte[128];
                var readResult = session.Stream.BeginRead(inputBuffer, 0, inputBuffer.Length, null, null);
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
                waitForNextRequestStopwatch.Stop();

                foreach (var req in inputBuffer)
                {
                    if (req == '0')
                        continue;

                    if (!sendStarted && (C.Equals(req) || NAK.Equals(req)))
                    {
                        use16bitChecksum = Options.HasFlag(FileTransferProtocolOptions.XmodemCrc) || C.Equals(req);
                        sendStarted = true;
                        waitToStartStopwatch.Stop();
                        lastPacket = GetNextPacketToSend(data, use16bitChecksum);
                        session.Io.OutputRaw(lastPacket);
                        session.Io.Flush();
                    }
                    else
                    {
                        switch (req)
                        {
                            case ACK:
                                lastPacket = GetNextPacketToSend(data, use16bitChecksum);
                                session.Io.OutputRaw(lastPacket);
                                session.Io.Flush();
                                break;
                            case NAK:
                                if (++errorCount > _maxErrors)
                                    canceled = true;
                                else
                                    session.Io.OutputRaw(lastPacket);
                                session.Io.Flush();
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
            } 

            return _offset >= Data.Length - 1;
        }

        /// <summary>
        /// Receives the Data and returns true if all data was received successfully.
        /// </summary>
        public bool Receive(BbsSession session, FileTransferProtocolOptions options = FileTransferProtocolOptions.None)
        {
            Options = options;

            _offset = 0;
            _data = new List<byte>();

            bool completed = false;
            bool completedSuccesfully = false;
            int errorCount = 0;
            int lastPacketNum = 0;
            int startRetries = 10;
            int packetSize = 3; // SOH, Packet Num, ~Packet Num
            packetSize += options.HasFlag(FileTransferProtocolOptions.Xmodem1k) ? 1024 : 128;
            packetSize += options.HasFlag(FileTransferProtocolOptions.XmodemCrc) ? 2 : 1;

            Stopwatch sw = new Stopwatch();
            byte nextResponse = C;

            do
            {
                byte[] inputBuffer = new byte[4096];

                // keep sending 'C' until we get a packet
                session.Io.OutputRaw(nextResponse);
                var readResult = session.Stream.BeginRead(inputBuffer, 0, inputBuffer.Length, null, null);
                sw.Reset();
                sw.Start();
                while (!readResult.IsCompleted)
                {
                    Thread.Sleep(25);
                    if (sw.Elapsed.TotalSeconds > 2)
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

                if (!inputBuffer.Any(b => b != 0))
                    continue;

                // while packet doesn't start with EOT or CAN ...

                var header = inputBuffer[0];
                switch (header)
                {
                    case SOH:
                    case STX:
                        if (header == STX && !Options.HasFlag(FileTransferProtocolOptions.Xmodem1k))
                            Options |= FileTransferProtocolOptions.Xmodem1k;
                        else if (header == SOH && Options.HasFlag(FileTransferProtocolOptions.Xmodem1k))
                            Options &= ~FileTransferProtocolOptions.Xmodem1k;

                        if (ProcessIncomingPacket(inputBuffer, ref lastPacketNum))
                            nextResponse = ACK;
                        else if (++errorCount > _maxErrors)
                        {
                            completed = true;
                            completedSuccesfully = false;
                            session.Io.OutputRaw(CAN);
                        }
                        else
                            nextResponse = NAK;
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
                    default:
                        if (nextResponse == ACK)
                            nextResponse = NAK;
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
            int payloadSize = Options.HasFlag(FileTransferProtocolOptions.Xmodem1k) ? 1024 : 128;
            var payload = inputBuffer.Skip(3).Take(payloadSize).ToArray();

            if (Options.HasFlag(FileTransferProtocolOptions.XmodemCrc))
            {
                // calculate checksum on the payload
                var crc = GetCrc16(payload);

                // verify that calculated checksum matches the checksum in the packet
                if (crc[0] != inputBuffer[payloadSize + 3] ||
                    crc[1] != inputBuffer[payloadSize + 4])
                {
                    return false;
                }
            }
            else
            {
                var checksum = Get8bitChecksum(payload);
                if (checksum != inputBuffer[payloadSize + 3])
                    return false;
            }

            _data.AddRange(payload);

            // increment lastPacketNum
            lastPacketNum = packetNumber;

            return true;
        }

        private byte[] GetNextPacketToSend(byte[] data, bool use16bitChecksum)
        {
            if (_offset >= data.Length)
                return new[] { EOT };

            int payloadSize = Options.HasFlag(FileTransferProtocolOptions.Xmodem1k) ? 1024 : 128;
            int checksumSize = use16bitChecksum ? 2 : 1;
            int packetSize = 3 + payloadSize + checksumSize;

            var packet = new byte[packetSize];
            packet[0] = Options.HasFlag(FileTransferProtocolOptions.Xmodem1k) ? STX : SOH;

            // packet numbers start at 1 but wrap around to 0 so increment before using
            _packetNum = (byte)((_packetNum + 1) % 256);
            packet[1] = _packetNum;
            packet[2] = (byte)~_packetNum;
            var payload = new byte[payloadSize];
            for (int i = 0; i < payloadSize; i++)
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
                packet[131] = Get8bitChecksum(payload);
            }

            return packet;
        }

        private static byte[] GetCrc16(byte[] data)
        {
            ushort c = CalculateCrc16(data);
            byte low = (byte)c;
            byte high = (byte)(c >> 8);
            var crc = new byte[] { high, low };
            return crc;
        }

        // Adapted from sample C code found at https://web.mit.edu/6.115/www/amulet/xmodem.htm
        private static ushort CalculateCrc16(byte[] data)
        {
            const int poly = 0x1021;

            //int crc = 0;
            //int count = data.Length;
            //int ptr = 0;
            //int i;

            //while (--count >= 0)
            //{
            //    crc ^= (data[ptr++] << 8);
            //    i = 8;
            //    do
            //    {
            //        if ((crc & 0x8000) == 0x8000)
            //            crc <<= 1 ^ 0x1021;
            //        else
            //            crc <<= 1;
            //    } while (--i > 0);
            //}

            //return crc;

            //int i;

            int crc = 0;
            int c = 0;

            for (int num = data.Length; num > 0; num--)
            {
                var addr = data[c++];
                crc = crc ^ (addr << 8);
                for (int i = 0; i < 8; i++)
                {
                    //crc2 = crc2 << 1;
                    //if ((crc2 & 0x10000) != 0)
                    //    crc2 = (crc2 ^ 0x1021) & 0xFFFF; 
                    
                    crc = ((crc & 0x8000) != 0) ? ((crc << 1) ^ poly) : crc << 1;
                }
            }

            return (ushort)(crc % 65536);
        }

        private static byte Get8bitChecksum(byte[] data)
        {
            return (byte)(data.Sum(b => b) % 256);
        }
    }
}
