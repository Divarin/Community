using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace miniBBS.Services.Services
{
    public class Xmodem : IFileTransferProtocol
    {
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
        /// Data Link Escape
        /// </summary>
        private const byte DLE = 0x10;

        /// <summary>
        /// X-On (DC1) Transmit On
        /// </summary>
        private const byte XON = 0x11;

        /// <summary>
        /// X-Off (DC3) Transmit Off
        /// </summary>
        private const byte XOFF = 0x13;

        /// <summary>
        /// Not Achnowledge
        /// </summary>
        private const byte NAK = 0x15;

        /// <summary>
        /// Synchronous idle
        /// </summary>
        private const byte SYN = 0x16;

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
        private const byte PAD = 0;
        #endregion

        const int MaxRetries = 25;

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
            byte ch = 0;
            byte packetNum = 1;
            int retries = 0;

            for (var t=0; t < MaxRetries; t++)
            {
                // wait for NAK from remote
                ch = session.Io.InputRaw().First();
                if (ch == NAK)
                    break;
                Thread.Sleep(1000);
            }

            if (ch != NAK)
            {
                return false;
            }

            var success = true;
            var sending = true;
            var retry = false;
            var dataOffset = 0;
            byte checksum = 0;
            byte[] packet = new byte[128];
            
            // Let's go!
            while (sending)
            {
                session.Io.OutputRaw(SOH);
                session.Io.OutputRaw(packetNum);
                session.Io.OutputRaw((byte)(255 - packetNum));

                if (!retry)
                {
                    checksum = 0;
                    // create packet, pad with 0's if last packet, also compute checksum for packet.
                    for (var i = 0; i < 128; i++)
                    {
                        packet[i] = dataOffset < Data.Length ? Data[dataOffset++] : PAD;
                        checksum += packet[i];
                    }

                }

                // send packet and checksum
                session.Io.OutputRaw(packet);
                session.Io.OutputRaw(checksum);
                retry = false;

                ch = session.Io.InputRaw().FirstOrDefault();
                if (ch == ACK)
                {
                    // packet received successfully
                    packetNum++;
                }
                else if (ch == NAK)
                {
                    // packet not received successfully
                    retry = true;
                    retries++;
                    Thread.Sleep(250);
                    if (retries > MaxRetries)
                    {
                        session.Io.OutputRaw(CAN, CAN, CAN);
                        sending = false;
                        success = false;
                        break;
                    }
                }
                else if (ch == CAN)
                {
                    sending = false;
                    break;
                }

                if (dataOffset >= Data.Length)
                {
                    // We are done!
                    session.Io.OutputRaw(EOT);

                    ch = session.Io.InputRaw().FirstOrDefault();
                    if (ch == NAK)
                    {
                        // send EOT again?
                        session.Io.OutputRaw(EOT);
                        ch = session.Io.InputRaw().FirstOrDefault();
                    }

                    if (ch == ACK)
                    {
                        session.Io.OutputRaw(ACK);
                        success = true;
                    }
                    else
                    {
                        session.Io.OutputRaw(CAN);
                        success = false;
                    }

                    sending = false;
                }
            } // end while (sending);

            return success;
        }

        /// <summary>
        /// Receives the Data and returns true if all data was received successfully.
        /// </summary>
        public bool Receive(BbsSession session)
        {
            byte[] packet = new byte[131];
            byte ch;
            byte checksum;
            var receiving = true;
            var success = true;
            var retries = 0;
            bool retry;
            var dataOffset = 0;
            int badBytes;
            byte packetNum = 1;
            byte[] input;

            // send NAK to initiate transfer
            session.Io.OutputRaw(NAK);

            while (receiving)
            {
                // get control byte
                input = session.Io.InputRaw();
                ch = input.FirstOrDefault(x => x == SOH || x == EOT);
                if (ch == SOH)
                {
                    // Start of Header (SOH) received
                    // get packet
                    badBytes = 0;
                    checksum = 0;
                    retry = false;

                    input = session.Io.InputRaw();
                    var inputOffset = 0;
                    for (var i=0; i < 131; i++)
                    {
                        ch = inputOffset < input.Length ? input[inputOffset++] : PAD;
                        packet[i] = ch;
                        if (i > 1 && i < 130)
                        {
                            checksum += ch;
                        }
                    }

                    if (badBytes == 0)
                    {
                        var expectedPacketNum = packet[0];
                        var expectedPacketNumCompliment = packet[1];
                        var expectedChecksum = packet[130];

                        // validate packet
                        if (expectedPacketNum == packetNum &&
                            (255-expectedPacketNumCompliment) == expectedPacketNum &&
                            checksum == expectedChecksum)
                        {
                            // It's good, save it!
                            for (var i = 2; i < 130; i++)
                            {
                                Data[dataOffset++] = packet[i];
                            }
                            packetNum++;
                            session.Io.OutputRaw(ACK);
                        }
                        else
                        {
                            badBytes = 1;
                        }
                    }

                    if (badBytes > 0)
                    {
                        // bad packet!
                        Thread.Sleep(250);
                        session.Io.OutputRaw(NAK);
                        retries++;
                        retry = true;

                        if (retries > MaxRetries)
                        {
                            session.Io.OutputRaw(CAN, CAN, CAN);
                            receiving = false;
                            success = false;
                            break;
                        }
                    }
                }
                else if (ch == EOT)
                {
                    // End of Transmission (EOT) received
                    session.Io.OutputRaw(ACK);
                    receiving = false;
                    success = true;
                }
                else
                {
                    // Cancel
                    session.Io.OutputRaw(CAN, CAN, CAN);
                    receiving = false;
                    success = false;
                    break;
                }
            } // end while (receiving)

            return success;
        }
    }
}
