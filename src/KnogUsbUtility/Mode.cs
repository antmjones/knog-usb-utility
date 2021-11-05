using System.Collections.Generic;

namespace KnogUsbUtility {
    public class Mode {
        public bool IsEnabled { get; set; } = true;
        public byte ShortDelay { get; set; }
        public byte LongDelay { get; set; }
        public IList<Step> Steps { get; set; } = new List<Step>();
    }
}
