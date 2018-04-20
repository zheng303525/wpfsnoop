// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Snoop.Infrastructure;

namespace Snoop
{
    /// <summary>
    /// 用于UI线程延迟处理回调函数
    /// </summary>
	public class DelayedCall
	{
        /// <summary>
        /// 处理回调函数
        /// </summary>
	    private readonly Action _handler;

        /// <summary>
        /// 调用方法优先级
        /// </summary>
	    private readonly DispatcherPriority _priority;

        /// <summary>
        /// 是否正在排队：1表示正在排队，0表示没有排队
        /// </summary>
	    private int _queued;

        public DelayedCall(Action handler, DispatcherPriority priority)
		{
			this._handler = handler;
			this._priority = priority;
		}

        /// <summary>
        /// 进入<see cref="Dispatcher"/>排队处理队列
        /// </summary>
		public void Enqueue()
		{
		    if (Interlocked.CompareExchange(ref _queued, 1, 0) == 0)
		    {
		        Dispatcher dispatcher;
		        if (Application.Current == null || SnoopModes.MultipleDispatcherMode)
		        {
		            dispatcher = Dispatcher.CurrentDispatcher;
		        }
		        else
		        {
		            dispatcher = Application.Current.Dispatcher;
		        }
		        dispatcher.BeginInvoke(this._priority, new DispatcherOperationCallback(this.Process), null);
            }
		}

        /// <summary>
        /// 开始执行回调函数
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
		private object Process(object arg)
		{
		    Interlocked.Exchange(ref _queued, 0);
			this._handler();
			return null;
		}
	}
}
