using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Echo.StellarisModMerge {
	public class InvalidArgumentTypeException : ArgumentException {
		protected InvalidArgumentTypeException(SerializationInfo info, StreamingContext context) : base(info, context) {}
		public InvalidArgumentTypeException(string message, string paramName, Exception innerException) : base(message, paramName, innerException) {}
		public InvalidArgumentTypeException(string message, string paramName) : base(message, paramName) {}
		public InvalidArgumentTypeException(string message, Exception innerException) : base(message, innerException) {}
		public InvalidArgumentTypeException(string message) : base(message) { }
		public InvalidArgumentTypeException() {}
	}
}
