using System;
using Nexus.Link.AsyncCaller.Sdk.Data.Models;

namespace UnitTests
{
    internal class RequestEnvelopeMock : RawRequestEnvelope
    {
        // To prevent the random delay for retries
        public new DateTimeOffset NextAttemptAt => DateTimeOffset.Now;
    }
}