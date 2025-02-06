namespace AMDevIT.Restling.Core.Network
{
    public static class HttpMediaType
    {
        #region Consts

        #region Application Media Types

        public const string ApplicationJson = "application/json";
        public const string ApplicationXml = "application/xml";
        public const string ApplicationOctetStream = "application/octet-stream";
        public const string ApplicationFormUrlEncoded = "application/x-www-form-urlencoded";
        public const string ApplicationPdf = "application/pdf";
        public const string ApplicationZip = "application/zip";
        public const string ApplicationJavascript = "application/javascript";
        public const string ApplicationProblemJson = "application/problem+json";
        public const string ApplicationProblemXml = "application/problem+xml";
        public const string ApplicationAtomXml = "application/atom+xml";

        #endregion

        #region Text Media Types

        public const string TextPlain = "text/plain";
        public const string TextHtml = "text/html";
        public const string TextCss = "text/css";
        public const string TextXml = "text/xml";
        public const string TextJavascript = "text/javascript"; // Deprecated, prefer "application/javascript"

        #endregion

        #region Image Media Types

        public const string ImagePng = "image/png";
        public const string ImageJpeg = "image/jpeg";
        public const string ImageGif = "image/gif";
        public const string ImageBmp = "image/bmp";
        public const string ImageSvgXml = "image/svg+xml";
        public const string ImageWebp = "image/webp";

        #endregion

        #region Audio Media Types

        public const string AudioMpeg = "audio/mpeg";
        public const string AudioOgg = "audio/ogg";
        public const string AudioWav = "audio/wav";
        public const string AudioWebm = "audio/webm";

        #endregion

        #region Video Media Types

        public const string VideoMp4 = "video/mp4";
        public const string VideoMpeg = "video/mpeg";
        public const string VideoOgg = "video/ogg";
        public const string VideoWebm = "video/webm";
        public const string VideoQuicktime = "video/quicktime";

        #endregion

        #region Multipart Media Types

        public const string MultipartFormData = "multipart/form-data";
        public const string MultipartMixed = "multipart/mixed";
        public const string MultipartAlternative = "multipart/alternative";

        #endregion

        #region Font Media Types

        public const string FontWoff = "font/woff";
        public const string FontWoff2 = "font/woff2";
        public const string FontOtf = "font/otf";
        public const string FontTtf = "font/ttf";
        public const string FontSvg = "font/svg";

        #endregion

        #endregion
    }
}
