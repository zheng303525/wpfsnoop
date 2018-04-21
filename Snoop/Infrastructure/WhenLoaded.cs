﻿// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;

namespace Snoop.Infrastructure
{
    /// <summary>
    /// Executes action on framework element when it's loaded.
    /// </summary>
    /// <remarks>
    /// Action is execued only once, no matter how many times Loaded event is fired.
    /// </remarks>
    public class WhenLoaded
    {
        private readonly FrameworkElement _target;
        private readonly Action<FrameworkElement> _loadedAction;

        public WhenLoaded(FrameworkElement target, Action<FrameworkElement> loadedAction)
        {
            this._target = target;
            this._loadedAction = loadedAction;

            DoAction();
        }

        private void DoAction()
        {
            if (this._target.IsLoaded)
            {
                this._loadedAction(this._target);
            }
            else
            {
                this._target.Loaded += TargetLoaded;
            }
        }

        private void TargetLoaded(object sender, RoutedEventArgs e)
        {
            this._target.Loaded -= TargetLoaded;
            this._loadedAction(this._target);
        }
    }

    /// <summary>
    /// <see cref="WhenLoaded"/> class wrapper to add clarity to the code.
    /// </summary>
    public static class WhenLoadedExtension
    {
        /// <summary>
        /// Executes given action only if framework element is loaded. Otherwise waits until framework element
        /// is loaded and executes the action.
        /// </summary>
        /// <param name="frameworkElement">Target framework element which we want to be loaded.</param>
        /// <param name="loadedAction">Action to be executed when framework element is loaded.</param>
        /// <remarks>Action is executed only once.</remarks>
        public static void WhenLoaded(this FrameworkElement frameworkElement, Action<FrameworkElement> loadedAction)
        {
            // The following object will not be GCed before its time.
            // * if frameworkElement is already loaded the ctor immediately exectues loadedAction and rests in piece
            // * otherwise framework element's Loaded event will keep the reference to event handler
            //   until it's loaded and and only then let it rest in piece.
            new WhenLoaded(frameworkElement, loadedAction);
        }
    }
}
