using System;
using System.Collections;

namespace XmlTests.Utils
{
    internal static class ListMergeUtils
    {
        /// <summary>
        /// Modifies the main list to combine it with the elements of the secondary list.
        /// The method used to combine is set using the mode parameter.
        /// The secondary list is not modified.
        /// </summary>
        public static void Combine(IList main, IList secondary, ListMergeMode mode)
        {
            if (main == null || secondary == null || secondary.Count == 0)
                return;

            if(main.IsFixedSize || main.IsReadOnly)
            {
                Console.WriteLine($"[ERROR] Main list is fixed size or read only.");
                return;
            }

            switch (mode)
            {
                case ListMergeMode.Replace:

                    // Clear list and add all values from other list.
                    main.Clear();
                    foreach (var value in secondary)
                        main.Add(value);

                    break;

                case ListMergeMode.Append:

                    // Simply add all values from secondary to primary.
                    foreach (var value in secondary)
                        main.Add(value);

                    break;

                case ListMergeMode.Merge:

                    // Add all values from other list, if they are not already in the list.
                    foreach (var value in secondary)
                        if(!main.Contains(value))
                            main.Add(value);

                    break;
                default:
                    Console.WriteLine($"[ERROR] {mode} is not implemented.");
                    break;
            }
        }
    }
}
