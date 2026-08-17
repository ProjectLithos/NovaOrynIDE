namespace System
{
    public class Object
    {
#pragma warning disable CS0169 // The NativeAOT object header is consumed by generated code, not referenced by C# source.
        private IntPtr _methodTable;
#pragma warning restore CS0169

        /// <summary>Returns the freestanding type name used when no more specific value formatter is available.</summary>
        public virtual String ToString() => "System.Object";
    }
    public struct Void { }
    public struct Boolean
    {
        /// <summary>Returns the normal .NET Boolean text without allocating a new string.</summary>
        public override String ToString() => this ? "True" : "False";
    }
    public struct Char { }
    public struct SByte { public const SByte MinValue = -128; public const SByte MaxValue = 127; }
    public struct Byte { public const Byte MinValue = 0; public const Byte MaxValue = 255; }
    public struct Int16 { public const Int16 MinValue = -32768; public const Int16 MaxValue = 32767; }
    public struct UInt16 { public const UInt16 MinValue = 0; public const UInt16 MaxValue = 65535; }
    public struct Int32 { public const Int32 MinValue = -2147483648; public const Int32 MaxValue = 2147483647; }
    public struct UInt32 { public const UInt32 MinValue = 0U; public const UInt32 MaxValue = 0xFFFFFFFFU; }
    public struct Int64 { public const Int64 MinValue = -9223372036854775808L; public const Int64 MaxValue = 9223372036854775807L; }
    public struct UInt64 { public const UInt64 MinValue = 0UL; public const UInt64 MaxValue = 0xFFFFFFFFFFFFFFFFUL; }
    public struct IntPtr { }
    public struct UIntPtr { }
    public struct Single { }
    public struct Double { }
    public abstract class ValueType { }
    public abstract class Enum : ValueType { }
    public abstract class Array { }
    internal class Array<T> : Array { }

    // .NET 10 ILC recognizes this CoreLib type as the bulk-reference-copy helper owner.
    // The bootstrap has no GC and must never perform a managed-reference bulk copy.
    public static class Buffer
    {
        internal static void BulkMoveWithWriteBarrier(ref Byte destination, ref Byte source, UIntPtr byteCount)
        {
            while (true) { }
        }
    }

    // .NET 10 ILC resolves unmanaged value-type block clears and copies through System.SpanHelpers.
    // These freestanding implementations deliberately avoid the GC and any external runtime library.
    internal static unsafe class SpanHelpers
    {
        internal static void ClearWithoutReferences(ref Byte destination, UIntPtr byteCount)
        {
            fixed (Byte* pointer = &destination)
            {
                UInt64 length = (UInt64)(nuint)byteCount;
                for (UInt64 index = 0; index < length; index++) pointer[index] = 0;
            }
        }

        internal static void Memmove(ref Byte destination, ref Byte source, UIntPtr byteCount)
        {
            fixed (Byte* destinationPointer = &destination)
            fixed (Byte* sourcePointer = &source)
            {
                UInt64 length = (UInt64)(nuint)byteCount;
                if (length == 0UL || destinationPointer == sourcePointer) return;

                UInt64 destinationAddress = (UInt64)(nuint)destinationPointer;
                UInt64 sourceAddress = (UInt64)(nuint)sourcePointer;
                if (destinationAddress < sourceAddress || destinationAddress - sourceAddress >= length)
                {
                    for (UInt64 index = 0; index < length; index++) destinationPointer[index] = sourcePointer[index];
                    return;
                }

                for (UInt64 index = length; index != 0UL; index--) destinationPointer[index - 1UL] = sourcePointer[index - 1UL];
            }
        }
    }
    public sealed unsafe class String
    {
#pragma warning disable CS0649 // NativeAOT materializes string objects and populates these runtime layout fields.
        private readonly Int32 _stringLength;
        private Char _firstChar;
#pragma warning restore CS0649

        public Int32 Length
        {
            get { return _stringLength; }
        }

        /// <summary>Returns this string instance.</summary>
        public override String ToString() => this;

        public Char this[Int32 index]
        {
            get
            {
                if ((UInt32)index >= (UInt32)_stringLength) return (Char)0;
                fixed (Char* firstCharacter = &_firstChar)
                {
                    return firstCharacter[index];
                }
            }
        }
    }
    public abstract class Delegate { }
    public abstract class MulticastDelegate : Delegate { }
    public class Attribute { }
    public enum AttributeTargets { }
    public sealed class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets targets) { }
        public Boolean AllowMultiple { get; set; }
        public Boolean Inherited { get; set; }
    }
    public struct RuntimeTypeHandle { }
    public struct RuntimeMethodHandle { }
    public struct RuntimeFieldHandle { }
    public struct Nullable<T> where T : struct { }

    namespace Reflection
    {
        public sealed class DefaultMemberAttribute : Attribute
        {
            public DefaultMemberAttribute(String memberName) { }
        }
    }

    namespace Runtime
    {
        public sealed class RuntimeExportAttribute : Attribute
        {
            public RuntimeExportAttribute(String name) { }
        }
    }

    namespace Runtime.CompilerServices
    {
        public sealed class CompilerGeneratedAttribute : Attribute { }
        public sealed class IsReadOnlyAttribute : Attribute { }
        public sealed class IsByRefLikeAttribute : Attribute { }
        public static class RuntimeFeature
        {
            public const String UnmanagedSignatureCallingConvention = nameof(UnmanagedSignatureCallingConvention);
        }
        public static class RuntimeHelpers
        {
            public static unsafe Int32 OffsetToStringData => sizeof(IntPtr) + sizeof(Int32);
        }
    }

    namespace Runtime.InteropServices
    {
        public enum CallingConvention { Winapi = 1, Cdecl = 2, StdCall = 3, ThisCall = 4, FastCall = 5 }
        public enum CharSet { None = 1, Ansi = 2, Unicode = 3, Auto = 4 }
        public enum LayoutKind { Sequential = 0, Explicit = 2, Auto = 3 }
        public sealed class StructLayoutAttribute : Attribute
        {
            public StructLayoutAttribute(LayoutKind kind) { Value = kind; }
            public CharSet CharSet;
            public Int32 Pack;
            public Int32 Size;
            public LayoutKind Value { get; }
        }
        public sealed class DllImportAttribute : Attribute
        {
            public DllImportAttribute(String libraryName) { }
            public String EntryPoint { get; set; }
            public CallingConvention CallingConvention { get; set; }
            public Boolean ExactSpelling { get; set; }
        }
    }
}

namespace Internal.Runtime.CompilerHelpers
{
    using System;
    using System.Runtime;

    internal static class StartupCodeHelpers
    {
        [RuntimeExport("RhpReversePInvoke")]
        private static void RhpReversePInvoke(IntPtr frame) { }

        [RuntimeExport("RhpReversePInvokeReturn")]
        private static void RhpReversePInvokeReturn(IntPtr frame) { }

        [RuntimeExport("RhpPInvoke")]
        private static void RhpPInvoke(IntPtr frame) { }

        [RuntimeExport("RhpPInvokeReturn")]
        private static void RhpPInvokeReturn(IntPtr frame) { }

        [RuntimeExport("RhpFallbackFailFast")]
        private static void RhpFallbackFailFast()
        {
            while (true) { }
        }
    }
}
