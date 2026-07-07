/*
 * Copyright 2026 Peter Han
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace PeterHan.PLib.Core {
	/// <summary>
	/// A helper class for migrating to boxed event types.
	/// </summary>
	/// <typeparam name="T">The value type to be unboxed.</typeparam>
	public static class AutoUnbox<T> where T : struct {
		/// <summary>
		/// Boxes the specified object.
		/// </summary>
		/// <param name="data">The data to box.</param>
		/// <returns>The boxed data for the Trigger method.</returns>
		public static object Box(T data) {
			return Boxed<T>.Get(data);
		}

		/// <summary>
		/// Unboxes the specified object.
		/// </summary>
		/// <param name="data">The data or boxed data.</param>
		/// <param name="result">The result of unboxing. Only valid if true is returned.</param>
		/// <returns>true if the data could be unboxed or directly converted, or false otherwise.</returns>
		public static bool Unbox(object data, out T result) {
			bool ok = true;
			if (data is Boxed<T> boxed)
				result = boxed.value;
			else if (data is T rawResult)
				result = rawResult;
			else {
				result = default;
				ok = false;
			}
			return ok;
		}
	}
}
