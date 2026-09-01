namespace AMDevIT.Restling.Core.Multipart
{
    /// <summary>Defines safety limits used while parsing multipart content.</summary>
    public sealed class MultipartOptions
    {
        #region Properties

        /// <summary>Maximum number of parts in each buffered multipart entity.</summary>
        public int MaxParts { get; init; } = 1000;

        /// <summary>Maximum header bytes allowed for one part.</summary>
        public int MaxHeaderBytes { get; init; } = 16 * 1024;

        /// <summary>Maximum number of nested multipart levels below the root.</summary>
        public int MaxNestingDepth { get; init; } = 8;

        /// <summary>Maximum buffered bytes allowed for one part or streamed mixed-replace frame.</summary>
        public long MaxPartBytes { get; init; } = 128L * 1024L * 1024L;

        #endregion

        #region Methods

        /// <summary>Validates all configured limits.</summary>
        internal void Validate()
        {
            if (this.MaxParts <= 0)
                throw new ArgumentOutOfRangeException(nameof(this.MaxParts));
            if (this.MaxHeaderBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(this.MaxHeaderBytes));
            if (this.MaxNestingDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(this.MaxNestingDepth));
            if (this.MaxPartBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(this.MaxPartBytes));
        }

        #endregion
    }
}
