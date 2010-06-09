﻿//-----------------------------------------------------------------------
// <copyright file="MessagePart.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.Messaging.Reflection {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Diagnostics.Contracts;
	using System.Globalization;
	using System.Linq;
	using System.Reflection;
	using System.Xml;
    using DotNetOpenAuth.Configuration;
#if !SILVERLIGHT
    using System.Net.Security;
	using DotNetOpenAuth.OpenId;
#endif
	/// <summary>
	/// Describes an individual member of a message and assists in its serialization.
	/// </summary>
	[ContractVerification(true)]
	[DebuggerDisplay("MessagePart {Name}")]
	internal class MessagePart {
		/// <summary>
		/// A map of converters that help serialize custom objects to string values and back again.
		/// </summary>
		private static readonly Dictionary<Type, ValueMapping> converters = new Dictionary<Type, ValueMapping>();

		/// <summary>
		/// A map of instantiated custom encoders used to encode/decode message parts.
		/// </summary>
		private static readonly Dictionary<Type, IMessagePartEncoder> encoders = new Dictionary<Type, IMessagePartEncoder>();

		/// <summary>
		/// The string-object conversion routines to use for this individual message part.
		/// </summary>
		private ValueMapping converter;

		/// <summary>
		/// The property that this message part is associated with, if aplicable.
		/// </summary>
		private PropertyInfo property;

		/// <summary>
		/// The field that this message part is associated with, if aplicable.
		/// </summary>
		private FieldInfo field;

		/// <summary>
		/// The type of the message part.  (Not the type of the message itself).
		/// </summary>
		private Type memberDeclaredType;

		/// <summary>
		/// The default (uninitialized) value of the member inherent in its type.
		/// </summary>
		private object defaultMemberValue;

		/// <summary>
		/// Initializes static members of the <see cref="MessagePart"/> class.
		/// </summary>
		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This simplifies the rest of the code.")]
		[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "By design.")]
		[SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Much more efficient initialization when we can call methods.")]
		static MessagePart() {
			Func<string, Uri> safeUri = str => {
				Contract.Assume(str != null);
				return new Uri(str);
			};
			Func<string, bool> safeBool = str => {
				Contract.Assume(str != null);
				return bool.Parse(str);
			};
#if !SILVERLIGHT
			Func<string, Identifier> safeIdentifier = str => {
				Contract.Assume(str != null);
				ErrorUtilities.VerifyFormat(str.Length > 0, MessagingStrings.NonEmptyStringExpected);
				return Identifier.Parse(str, true);
			};
#endif
			Func<byte[], string> safeFromByteArray = bytes => {
				Contract.Assume(bytes != null);
				return Convert.ToBase64String(bytes);
			};
			Func<string, byte[]> safeToByteArray = str => {
				Contract.Assume(str != null);
				return Convert.FromBase64String(str);
			};
#if !SILVERLIGHT
			Func<string, Realm> safeRealm = str => {
				Contract.Assume(str != null);
				return new Realm(str);
			};
			Map<Uri>(uri => uri.AbsoluteUri, uri => uri.OriginalString, safeUri);
			Map<DateTime>(dt => XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Utc), null, str => XmlConvert.ToDateTime(str, XmlDateTimeSerializationMode.Utc));
			Map<byte[]>(safeFromByteArray, null, safeToByteArray);
			Map<Realm>(realm => realm.ToString(), realm => realm.OriginalString, safeRealm);
			Map<Identifier>(id => id.SerializedString, id => id.OriginalString, safeIdentifier);
			Map<bool>(value => value.ToString().ToLowerInvariant(), null, safeBool);
			Map<CultureInfo>(c => c.Name, null, str => new CultureInfo(str));
			Map<CultureInfo[]>(cs => string.Join(",", cs.Select(c => c.Name).ToArray()), null, str => str.Split(',').Select(s => new CultureInfo(s)).ToArray());
#endif
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessagePart"/> class.
		/// </summary>
		/// <param name="member">
		/// A property or field of an <see cref="IMessage"/> implementing type
		/// that has a <see cref="MessagePartAttribute"/> attached to it.
		/// </param>
		/// <param name="attribute">
		/// The attribute discovered on <paramref name="member"/> that describes the
		/// serialization requirements of the message part.
		/// </param>
		[SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Code contracts requires it.")]
		internal MessagePart(MemberInfo member, MessagePartAttribute attribute) {
			Contract.Requires<ArgumentNullException>(member != null);
			Contract.Requires<ArgumentException>(member is FieldInfo || member is PropertyInfo);
			Contract.Requires<ArgumentNullException>(attribute != null);

			this.field = member as FieldInfo;
			this.property = member as PropertyInfo;
			this.Name = attribute.Name ?? member.Name;
#if !SILVERLIGHT
			this.RequiredProtection = attribute.RequiredProtection;
#endif
			this.IsRequired = attribute.IsRequired;
			this.AllowEmpty = attribute.AllowEmpty;
			this.memberDeclaredType = (this.field != null) ? this.field.FieldType : this.property.PropertyType;
			this.defaultMemberValue = DeriveDefaultValue(this.memberDeclaredType);

			Contract.Assume(this.memberDeclaredType != null); // CC missing PropertyInfo.PropertyType ensures result != null
			if (attribute.Encoder == null) {
				if (!converters.TryGetValue(this.memberDeclaredType, out this.converter)) {
					if (this.memberDeclaredType.IsGenericType &&
						this.memberDeclaredType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
						// It's a nullable type.  Try again to look up an appropriate converter for the underlying type.
						Type underlyingType = Nullable.GetUnderlyingType(this.memberDeclaredType);
						ValueMapping underlyingMapping;
						if (converters.TryGetValue(underlyingType, out underlyingMapping)) {
							this.converter = new ValueMapping(
								underlyingMapping.ValueToString,
								null,
								str => str != null ? underlyingMapping.StringToValue(str) : null);
						} else {
							this.converter = new ValueMapping(
								obj => obj != null ? obj.ToString() : null,
								null,
								str => str != null ? Convert.ChangeType(str, underlyingType, CultureInfo.InvariantCulture) : null);
						}
					} else {
						this.converter = new ValueMapping(
							obj => obj != null ? obj.ToString() : null,
							null,
							str => str != null ? Convert.ChangeType(str, this.memberDeclaredType, CultureInfo.InvariantCulture) : null);
					}
				}
			} else {
				this.converter = new ValueMapping(GetEncoder(attribute.Encoder));
			}

			// readonly and const fields are considered legal, and "constants" for message transport.
			FieldAttributes constAttributes = FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault;
			if (this.field != null && (
				(this.field.Attributes & FieldAttributes.InitOnly) == FieldAttributes.InitOnly ||
				(this.field.Attributes & constAttributes) == constAttributes)) {
				this.IsConstantValue = true;
			} else if (this.property != null && !this.property.CanWrite) {
				this.IsConstantValue = true;
			}

			// Validate a sane combination of settings
			this.ValidateSettings();
		}

		/// <summary>
		/// Gets or sets the name to use when serializing or deserializing this parameter in a message.
		/// </summary>
		internal string Name { get; set; }
#if !SILVERLIGHT
		/// <summary>
		/// Gets or sets whether this message part must be signed.
		/// </summary>
		internal ProtectionLevel RequiredProtection { get; set; }
#endif
		/// <summary>
		/// Gets or sets a value indicating whether this message part is required for the
		/// containing message to be valid.
		/// </summary>
		internal bool IsRequired { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the string value is allowed to be empty in the serialized message.
		/// </summary>
		internal bool AllowEmpty { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the field or property must remain its default value.
		/// </summary>
		internal bool IsConstantValue { get; set; }

		/// <summary>
		/// Sets the member of a given message to some given value.
		/// Used in deserialization.
		/// </summary>
		/// <param name="message">The message instance containing the member whose value should be set.</param>
		/// <param name="value">The string representation of the value to set.</param>
		internal void SetValue(IMessage message, string value) {
			Contract.Requires<ArgumentNullException>(message != null);

			try {
				if (this.IsConstantValue) {
					string constantValue = this.GetValue(message);
					var caseSensitivity = DotNetOpenAuthSection.Configuration.Messaging.Strict ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
					if (!string.Equals(constantValue, value, caseSensitivity)) {
						throw new ArgumentException(string.Format(
							CultureInfo.CurrentCulture,
							MessagingStrings.UnexpectedMessagePartValueForConstant,
							message.GetType().Name,
							this.Name,
							constantValue,
							value));
					}
				} else {
					if (this.property != null) {
						this.property.SetValue(message, this.ToValue(value), null);
					} else {
						this.field.SetValue(message, this.ToValue(value));
					}
				}
			} catch (Exception ex) {
				throw ErrorUtilities.Wrap(ex, MessagingStrings.MessagePartReadFailure, message.GetType(), this.Name, value);
			}
		}

		/// <summary>
		/// Gets the normalized form of a value of a member of a given message.
		/// Used in serialization.
		/// </summary>
		/// <param name="message">The message instance to read the value from.</param>
		/// <returns>The string representation of the member's value.</returns>
		internal string GetValue(IMessage message) {
			try {
				object value = this.GetValueAsObject(message);
				return this.ToString(value, false);
			} catch (FormatException ex) {
				throw ErrorUtilities.Wrap(ex, MessagingStrings.MessagePartWriteFailure, message.GetType(), this.Name);
			}
		}

		/// <summary>
		/// Gets the value of a member of a given message.
		/// Used in serialization.
		/// </summary>
		/// <param name="message">The message instance to read the value from.</param>
		/// <param name="originalValue">A value indicating whether the original value should be retrieved (as opposed to a normalized form of it).</param>
		/// <returns>The string representation of the member's value.</returns>
		internal string GetValue(IMessage message, bool originalValue) {
			try {
				object value = this.GetValueAsObject(message);
				return this.ToString(value, originalValue);
			} catch (FormatException ex) {
				throw ErrorUtilities.Wrap(ex, MessagingStrings.MessagePartWriteFailure, message.GetType(), this.Name);
			}
		}

		/// <summary>
		/// Gets whether the value has been set to something other than its CLR type default value.
		/// </summary>
		/// <param name="message">The message instance to check the value on.</param>
		/// <returns>True if the value is not the CLR default value.</returns>
		internal bool IsNondefaultValueSet(IMessage message) {
			if (this.memberDeclaredType.IsValueType) {
				return !this.GetValueAsObject(message).Equals(this.defaultMemberValue);
			} else {
				return this.defaultMemberValue != this.GetValueAsObject(message);
			}
		}

		/// <summary>
		/// Figures out the CLR default value for a given type.
		/// </summary>
		/// <param name="type">The type whose default value is being sought.</param>
		/// <returns>Either null, or some default value like 0 or 0.0.</returns>
		private static object DeriveDefaultValue(Type type) {
			if (type.IsValueType) {
				return Activator.CreateInstance(type);
			} else {
				return null;
			}
		}

		/// <summary>
		/// Adds a pair of type conversion functions to the static conversion map.
		/// </summary>
		/// <typeparam name="T">The custom type to convert to and from strings.</typeparam>
		/// <param name="toString">The function to convert the custom type to a string.</param>
		/// <param name="toOriginalString">The mapping function that converts some custom value to its original (non-normalized) string.  May be null if the same as the <paramref name="toString"/> function.</param>
		/// <param name="toValue">The function to convert a string to the custom type.</param>
		private static void Map<T>(Func<T, string> toString, Func<T, string> toOriginalString, Func<string, T> toValue) {
			Contract.Requires<ArgumentNullException>(toString != null, "toString");
			Contract.Requires<ArgumentNullException>(toValue != null, "toValue");

			if (toOriginalString == null) {
				toOriginalString = toString;
			}

			Func<object, string> safeToString = obj => obj != null ? toString((T)obj) : null;
			Func<object, string> safeToOriginalString = obj => obj != null ? toOriginalString((T)obj) : null;
			Func<string, object> safeToT = str => str != null ? toValue(str) : default(T);
			converters.Add(typeof(T), new ValueMapping(safeToString, safeToOriginalString, safeToT));
		}

		/// <summary>
		/// Checks whether a type is a nullable value type (i.e. int?)
		/// </summary>
		/// <param name="type">The type in question.</param>
		/// <returns>True if this is a nullable value type.</returns>
		private static bool IsNonNullableValueType(Type type) {
			if (!type.IsValueType) {
				return false;
			}

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// Retrieves a previously instantiated encoder of a given type, or creates a new one and stores it for later retrieval as well.
		/// </summary>
		/// <param name="messagePartEncoder">The message part encoder type.</param>
		/// <returns>An instance of the desired encoder.</returns>
		private static IMessagePartEncoder GetEncoder(Type messagePartEncoder) {
			Contract.Requires<ArgumentNullException>(messagePartEncoder != null);
			Contract.Ensures(Contract.Result<IMessagePartEncoder>() != null);

			IMessagePartEncoder encoder;
			if (!encoders.TryGetValue(messagePartEncoder, out encoder)) {
				encoder = encoders[messagePartEncoder] = (IMessagePartEncoder)Activator.CreateInstance(messagePartEncoder);
			}

			return encoder;
		}

		/// <summary>
		/// Converts a string representation of the member's value to the appropriate type.
		/// </summary>
		/// <param name="value">The string representation of the member's value.</param>
		/// <returns>
		/// An instance of the appropriate type for setting the member.
		/// </returns>
		private object ToValue(string value) {
			return this.converter.StringToValue(value);
		}

		/// <summary>
		/// Converts the member's value to its string representation.
		/// </summary>
		/// <param name="value">The value of the member.</param>
		/// <param name="originalString">A value indicating whether a string matching the originally decoded string should be returned (as opposed to a normalized string).</param>
		/// <returns>
		/// The string representation of the member's value.
		/// </returns>
		private string ToString(object value, bool originalString) {
			return originalString ? this.converter.ValueToOriginalString(value) : this.converter.ValueToString(value);
		}

		/// <summary>
		/// Gets the value of the message part, without converting it to/from a string.
		/// </summary>
		/// <param name="message">The message instance to read from.</param>
		/// <returns>The value of the member.</returns>
		private object GetValueAsObject(IMessage message) {
			if (this.property != null) {
				return this.property.GetValue(message, null);
			} else {
				return this.field.GetValue(message);
			}
		}

		/// <summary>
		/// Validates that the message part and its attribute have agreeable settings.
		/// </summary>
		/// <exception cref="ArgumentException">
		/// Thrown when a non-nullable value type is set as optional.
		/// </exception>
		private void ValidateSettings() {
			if (!this.IsRequired && IsNonNullableValueType(this.memberDeclaredType)) {
				MemberInfo member = (MemberInfo)this.field ?? this.property;
				throw new ArgumentException(
					string.Format(
						CultureInfo.CurrentCulture,
						"Invalid combination: {0} on message type {1} is a non-nullable value type but is marked as optional.",
						member.Name,
						member.DeclaringType));
			}
		}
	}
}
