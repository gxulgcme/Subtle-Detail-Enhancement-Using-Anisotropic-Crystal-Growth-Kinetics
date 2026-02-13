using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRImageFilters
{
    public static class IntPtrExtensions
    {
        /// <summary>
        /// Get pointer
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static unsafe IntPtr ToIntPtr(this sbyte[] obj)
        {
            IntPtr PtrA = IntPtr.Zero;
            fixed (sbyte* Ap = obj) return new IntPtr(Ap);
        }
        /// <summary>
        /// Get pointer
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static unsafe IntPtr ToIntPtr(this byte[] obj)
        {
            IntPtr PtrA = IntPtr.Zero;
            fixed (byte* Ap = obj) return new IntPtr(Ap);
        }
        /// <summary>
        /// Get pointer
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static unsafe IntPtr ToIntPtr(this short[] obj)
        {
            IntPtr PtrA = IntPtr.Zero;
            fixed (short* Ap = obj) return new IntPtr(Ap);
        }
        /// <summary>
        /// Get pointer
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static unsafe IntPtr ToIntPtr(this ushort[] obj)
        {
            IntPtr PtrA = IntPtr.Zero;
            fixed (ushort* Ap = obj) return new IntPtr(Ap);
        }
        /// <summary>
        /// Get pointer
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static unsafe IntPtr ToIntPtr(this int[] obj)
        {
            IntPtr PtrA = IntPtr.Zero;
            fixed (int* Ap = obj) return new IntPtr(Ap);
        }
        /// <summary>
        /// Get pointer
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static unsafe IntPtr ToIntPtr(this uint[] obj)
        {
            IntPtr PtrA = IntPtr.Zero;
            fixed (uint* Ap = obj) return new IntPtr(Ap);
        }
        public static unsafe IntPtr ToIntPtr(this float[] obj)
        {
            IntPtr PtrA = IntPtr.Zero;
            fixed (float* Ap = obj) return new IntPtr(Ap);
        }
        public static unsafe IntPtr ToIntPtr(this double[] obj)
        {
            IntPtr PtrA = IntPtr.Zero;
            fixed (double* Ap = obj) return new IntPtr(Ap);
        }
    }
}