using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;

namespace miniBBS.Core.Interfaces
{
    public interface IFileTransferProtocol
    {
        /// <summary>
        /// The data to be sent when calling Send or the buffer to place the data when calling Receive
        /// </summary>
        byte[] Data { get; set; }

        /// <summary>
        /// Sends the Data and returns true if all data was sent successfully.
        /// </summary>
        bool Send(BbsSession session, FileTransferProtocolOptions options = FileTransferProtocolOptions.None);

        /// <summary>
        /// Receives the Data and returns true if all data was received successfully.
        /// </summary>
        bool Receive(BbsSession session, FileTransferProtocolOptions options = FileTransferProtocolOptions.None);
    }
}
