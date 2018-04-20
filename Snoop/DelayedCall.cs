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
    /// ����UI�߳��ӳٴ���ص�����
    /// </summary>
	public class DelayedCall
	{
        /// <summary>
        /// ����ص�����
        /// </summary>
	    private readonly Action _handler;

        /// <summary>
        /// ���÷������ȼ�
        /// </summary>
	    private readonly DispatcherPriority _priority;

        /// <summary>
        /// �Ƿ������Ŷӣ�1��ʾ�����Ŷӣ�0��ʾû���Ŷ�
        /// </summary>
	    private int _queued;

        public DelayedCall(Action handler, DispatcherPriority priority)
		{
			this._handler = handler;
			this._priority = priority;
		}

        /// <summary>
        /// ����<see cref="Dispatcher"/>�ŶӴ������
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
        /// ��ʼִ�лص�����
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
