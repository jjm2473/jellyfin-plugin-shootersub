using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.MeiamSub.Shooter.Model
{
    /// <summary>
    /// SubtitleResponseRoot.
    /// </summary>
    public class SubtitleResponseRoot
    {
        /// <summary>
        /// Gets or sets SubtitleResponseRoot.
        /// </summary>
        public string Desc { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets SubtitleResponseRoot.
        /// </summary>
        public int Delay { get; set; } = 0;

        /// <summary>
        /// Gets or sets SubtitleResponseRoot.
        /// </summary>
        public IReadOnlyList<SubFileInfo> Files { get; set; } = Array.Empty<SubFileInfo>();
    }
}
