using System;

namespace KnogUsbUtility {
    public class DeviceCommunicationException : Exception {
        public DeviceCommunicationException() {
        }

        public DeviceCommunicationException(string message)
            : base(message) {
        }

        public DeviceCommunicationException(string message, Exception innerException)
            : base(message, innerException) {
        }
    }
}
