using System;
using System.Threading;

namespace NSubstitute.Core
{
#if NET45
	/// <summary>
	/// Delegates to ThreadLocal&lt;T&gt;, but wraps Value property access in try/catch to swallow ObjectDisposedExceptions.
	/// These can occur if the Value property is accessed from the finalizer thread. Because we can't detect this, we'll
	/// just swallow the exception (the finalizer thread won't be using any of the values from thread local storage anyway).
	/// </summary>
	/// <typeparam name="T"></typeparam>
	class RobustThreadLocal<T>
    {
        readonly ThreadLocal<T> _threadLocal;
        public RobustThreadLocal() { _threadLocal = new ThreadLocal<T>(); }
        public RobustThreadLocal(Func<T> initialValue) { _threadLocal = new ThreadLocal<T>(initialValue); }
        public T Value
        {
            get
            {
                try { return _threadLocal.Value; }
                catch (ObjectDisposedException) { return default(T); }
            }
            set
            {
                try { _threadLocal.Value = value; }
                catch (ObjectDisposedException) { }
            }
        }
    }
#else
	using System.Collections.Concurrent;
	class RobustThreadLocal<T>
	{
		readonly ConcurrentDictionary<string, T> contextMappings = new ConcurrentDictionary<string, T>();
		readonly Func<T> initialValue;
		readonly AsyncLocal<string> mappingKeyAsyncLocal;

		public RobustThreadLocal()
			: this(() => default(T)) { }

		public RobustThreadLocal(Func<T> initialValue)
		{
			this.initialValue = initialValue;

			mappingKeyAsyncLocal = new AsyncLocal<string>
			{
				Value = null
			};
		}

		string GetMappingKey()
		{
			var mappingKey = mappingKeyAsyncLocal.Value;
			if (mappingKey == null)
			{
				mappingKey = Guid.NewGuid().ToString();
				mappingKeyAsyncLocal.Value = mappingKey;
			}

			return mappingKey;
		}

		public T Value
		{
			get
			{
				var mappingKey = GetMappingKey();
				return contextMappings.GetOrAdd(mappingKey, _ => initialValue());
			}
			set
			{
				var mappingKey = GetMappingKey();
				contextMappings[mappingKey] = value;
			}
		}
	}
#endif
}
