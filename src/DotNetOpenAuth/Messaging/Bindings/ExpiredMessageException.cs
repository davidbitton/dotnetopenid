﻿//-----------------------------------------------------------------------
// <copyright file="ExpiredMessageException.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.Messaging.Bindings {
	using System;
	using System.Diagnostics.Contracts;
	using System.Globalization;

	/// <summary>
	/// An exception thrown when a message is received that exceeds the maximum message age limit.
	/// </summary>
#if !SILVERLIGHT
	[Serializable]
#endif
	internal class ExpiredMessageException : ProtocolException {
		/// <summary>
		/// Initializes a new instance of the <see cref="ExpiredMessageException"/> class.
		/// </summary>
		/// <param name="utcExpirationDate">The date the message expired.</param>
		/// <param name="faultedMessage">The expired message.</param>
		public ExpiredMessageException(DateTime utcExpirationDate, IProtocolMessage faultedMessage)
			: base(string.Format(CultureInfo.CurrentCulture, MessagingStrings.ExpiredMessage, utcExpirationDate.ToLocalTime(), DateTime.Now), faultedMessage) {
			Contract.Requires<ArgumentException>(utcExpirationDate.Kind == DateTimeKind.Utc);
		}
#if !SILVERLIGHT
		/// <summary>
		/// Initializes a new instance of the <see cref="ExpiredMessageException"/> class.
		/// </summary>
		/// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> 
		/// that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The System.Runtime.Serialization.StreamingContext 
		/// that contains contextual information about the source or destination.</param>
		protected ExpiredMessageException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
#endif
	}
}
