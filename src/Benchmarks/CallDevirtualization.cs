using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
	public class CallDevirtualization
	{
		private IVirtualInterface _intfImpl = null!;
		private ClassImpl _classImpl = null!;
		private StructImpl _structImpl;

		private BypassImpl<ClassImpl> _bypassImpl;

		[GlobalSetup]
		public void Setup()
		{
			_classImpl = new ClassImpl();
			_structImpl = new StructImpl();
			_bypassImpl = new BypassImpl<ClassImpl>(_classImpl);
			_intfImpl = _classImpl;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Trigger<T>(T impl) where T: IVirtualInterface => impl.Method();
		
		[Benchmark]
		public void InterfaceCall() { Trigger(_intfImpl); }

		[Benchmark]
		public void ClassCall() { Trigger(_classImpl); }

		[Benchmark]
		public void StructCall() { Trigger(_structImpl); }

		[Benchmark]
		public void BypassCall() { Trigger(_bypassImpl); }
		
		public interface IVirtualInterface
		{
			void Method();
		}

		public class ClassImpl: IVirtualInterface
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Method() { }
		}

		public readonly struct StructImpl: IVirtualInterface
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Method() { }
		}

		public readonly struct BypassImpl<TClass>: IVirtualInterface
			where TClass: class, IVirtualInterface
		{
			private readonly TClass _instance;

			public BypassImpl(TClass instance) { _instance = instance; }

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Method() => _instance.Method();
		}

	}
}
