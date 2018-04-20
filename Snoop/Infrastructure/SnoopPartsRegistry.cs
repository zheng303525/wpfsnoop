﻿// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.Generic;
using System.Windows.Media;

namespace Snoop.Infrastructure
{
    /// <summary>
    /// This service allows Snoop to mark certain visuals as visual tree roots of its own UI.
    /// </summary>
    public static class SnoopPartsRegistry
    {
        private static readonly List<Visual> RegisteredSnoopVisualTreeRoots = new List<Visual>();

        /// <summary>
        /// Adds given visual as a root of Snoop visual tree.
        /// </summary>
        internal static void AddSnoopVisualTreeRoot(Visual root)
        {
            if (!RegisteredSnoopVisualTreeRoots.Contains(root))
            {
                RegisteredSnoopVisualTreeRoots.Add(root);
            }
        }

        /// <summary>
        /// Opts out given visual from being considered as a Snoop's visual tree root.
        /// </summary>
        internal static void RemoveSnoopVisualTreeRoot(Visual root)
        {
            RegisteredSnoopVisualTreeRoots.Remove(root);
        }

        /// <summary>
        /// Checks whether given visual is a part of Snoop's visual tree.
        /// </summary>
        /// <param name="visual">Visual under question</param>
        /// <returns>True if visual belongs to the Snoop's visual tree; False otherwise.</returns>
        public static bool IsPartOfSnoopVisualTree(this Visual visual)
        {
            if (visual == null) return false;

            foreach (var snoopVisual in RegisteredSnoopVisualTreeRoots)
            {
                if (ReferenceEquals(visual, snoopVisual) || visual.Dispatcher == snoopVisual.Dispatcher && visual.IsDescendantOf(snoopVisual))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
