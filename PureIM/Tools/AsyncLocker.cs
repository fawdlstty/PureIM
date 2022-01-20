using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PureIM.Tools {
	class AsyncSemaphore {
		private readonly static Task s_completed = Task.FromResult (true);
		private readonly Queue<TaskCompletionSource<bool>> m_waiters = new Queue<TaskCompletionSource<bool>> ();
		private int m_currentCount;

		public AsyncSemaphore (int initialCount) {
			if (initialCount < 0)
				throw new ArgumentOutOfRangeException ("initialCount");
			m_currentCount = initialCount;
		}

		public Task WaitAsync () {
			lock (m_waiters) {
				if (m_currentCount > 0) {
					--m_currentCount;
					return s_completed;
				} else {
					var waiter = new TaskCompletionSource<bool> ();
					m_waiters.Enqueue (waiter);
					return waiter.Task;
				}
			}
		}

		public void Release () {
			TaskCompletionSource<bool> toRelease = null;
			lock (m_waiters) {
				if (m_waiters.Count > 0)
					toRelease = m_waiters.Dequeue ();
				else
					++m_currentCount;
			}
			if (toRelease != null)
				toRelease.SetResult (true);
		}
	}

	public class AsyncLocker {
		private AsyncSemaphore m_semaphore { init; get; }
		private Task<Releaser> m_releaser { init; get; }

		public AsyncLocker () {
			m_semaphore = new AsyncSemaphore (1);
			m_releaser = Task.FromResult (new Releaser (this));
		}

		public Task<Releaser> LockAsync () {
			var _wait = m_semaphore.WaitAsync ();
			return _wait.IsCompleted ? m_releaser : _wait.ContinueWith ((_, state) => new Releaser ((AsyncLocker) state), this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		public struct Releaser: IDisposable {
			private readonly AsyncLocker m_toRelease;
			internal Releaser (AsyncLocker toRelease) { m_toRelease = toRelease; }
			public void Dispose () {
				if (m_toRelease != null)
					m_toRelease.m_semaphore.Release ();
			}
		}
	}
}
