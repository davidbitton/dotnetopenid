﻿//-----------------------------------------------------------------------
// <copyright file="ExtensionsInteropHelperRPResponseTests.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.Test.OpenId.Extensions {
	using System.Collections.Generic;
	using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.OpenId;
	using DotNetOpenAuth.OpenId.Extensions;
	using DotNetOpenAuth.OpenId.Extensions.AttributeExchange;
	using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
	using DotNetOpenAuth.OpenId.Messages;
	using DotNetOpenAuth.OpenId.RelyingParty;
	using NUnit.Framework;

	[TestFixture]
	public class ExtensionsInteropHelperRPResponseTests : OpenIdTestBase {
		private IAuthenticationResponse response;
		private IList<IExtensionMessage> extensions;

		[SetUp]
		public override void SetUp() {
			base.SetUp();

			IndirectSignedResponse responseMessage = new IndirectSignedResponse(Protocol.Default.Version, RPUri);
			this.extensions = responseMessage.Extensions;
			this.response = new DotNetOpenAuth.OpenId.RelyingParty.PositiveAnonymousResponse(responseMessage);
		}

		/// <summary>
		/// Verifies that with no extensions present, UnifyExtensionsAsSreg returns an empty ClaimsResponse.
		/// </summary>
		[TestCase]
		public void UnifyExtensionsAsSregNoExtensions() {
			var sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg(this.response, true);
			Assert.IsNotNull(sreg);
			Assert.IsNull(sreg.Nickname);
		}

		/// <summary>
		/// Verifies that with sreg and AX extensions present, the sreg extension is returned.
		/// </summary>
		[TestCase]
		public void UnifyExtensionsAsSregWithSreg() {
			var sregInjected = new ClaimsResponse {
				Nickname = "andy",
			};
			var axInjected = new FetchResponse();
			axInjected.Attributes.Add(WellKnownAttributes.Name.Alias, "nate");
			this.extensions.Add(sregInjected);
			this.extensions.Add(axInjected);
			var sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg(this.response, true);
			Assert.AreSame(sregInjected, sreg);
			Assert.AreEqual("andy", sreg.Nickname);
		}

		/// <summary>
		/// Verifies UnifyExtensionsAsSreg correctly converts AX to sreg.
		/// </summary>
		[TestCase]
		public void UnifyExtensionsAsSregFromAXSchemaOrg() {
			var axInjected = new FetchResponse();
			axInjected.Attributes.Add(WellKnownAttributes.Name.Alias, "nate");
			this.extensions.Add(axInjected);
			var sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg(this.response, true);
			Assert.AreEqual("nate", sreg.Nickname);
		}

		/// <summary>
		/// Verifies UnifyExtensionsAsSreg correctly converts AX in a non-standard format to sreg.
		/// </summary>
		[TestCase]
		public void UnifyExtensionsasSregFromSchemaOpenIdNet() {
			var axInjected = new FetchResponse();
			axInjected.Attributes.Add(ExtensionsInteropHelper_Accessor.TransformAXFormat(WellKnownAttributes.Name.Alias, AXAttributeFormats.SchemaOpenIdNet), "nate");
			this.extensions.Add(axInjected);
			var sreg = ExtensionsInteropHelper.UnifyExtensionsAsSreg(this.response, true);
			Assert.AreEqual("nate", sreg.Nickname);
		}
	}
}
