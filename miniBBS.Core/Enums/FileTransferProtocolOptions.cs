using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum FileTransferProtocolOptions
    {   
        None = 0,

        /// <summary>
        /// X-Modem Option: Use a 16 bit CRC instead of an 8 bit checksum
        /// </summary>
        XmodemCrc = 1,

        /// <summary>
        /// X-Modem Option: Use 1024 byte payload instead of 128 byte payload
        /// </summary>
        Xmodem1k = 2
    }
}
