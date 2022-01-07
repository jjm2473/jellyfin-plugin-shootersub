using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.MeiamSub.Thunder.Model
{
    /// <summary>
    /// SubtitleResponseRoot.
    /// </summary>
    public class SubtitleResponseRoot
    {
        /// <summary>
        ///
        /// </summary>
        public List<SublistItem> sublist { get; set; }
    }
}
