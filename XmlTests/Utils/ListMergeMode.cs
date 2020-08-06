namespace XmlTests.Utils
{
    public enum ListMergeMode
    {
        /// <summary>
        /// The values in the new list are added to the old list.
        /// Duplicate items are discarded.
        /// </summary>
        Merge,

        /// <summary>
        /// The values in the new list are added to the old list.
        /// Items may included more than once.
        /// </summary>
        Append,

        /// <summary>
        /// The values in the new list replace the values from the old list.
        /// </summary>
        Replace
    }
}
