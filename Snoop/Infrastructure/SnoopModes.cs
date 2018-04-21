// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure
{
    public static class SnoopModes
    {
        /// <summary>
        /// Whether Snoop is Snooping in a situation where there are multiple dispatchers.
        /// The main Snoop UI is needed for each dispatcher.
        /// </summary>
        public static bool MultipleDispatcherMode { get; set; }

        /// <summary>
        /// 吞噬异常
        /// </summary>
        public static bool SwallowExceptions { get; set; }

        /// <summary>
        /// 忽略异常，即不处理异常
        /// </summary>
        public static bool IgnoreExceptions { get; set; }
    }
}
