using System;
using System.Collections.Generic;
using System.Text;

namespace Emby.MeiamSub.Thunder.Model
{
    /// <summary>
    /// DownloadSubInfo.
    /// </summary>
    public class DownloadSubInfo
    {
        /// <summary>
        /// Gets or sets DownloadSubInfo.Url.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets DownloadSubInfo.Format.
        /// </summary>
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets DownloadSubInfo.Language.
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets DownloadSubInfo.TwoLetterISOLanguageName.
        /// </summary>
        public string TwoLetterISOLanguageName { get; set; } = string.Empty;
    }
}
