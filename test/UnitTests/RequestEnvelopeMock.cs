using System;
using Xlent.Lever.AsyncCaller.Data.Models;

namespace UnitTests
{
    internal class RequestEnvelopeMock : RequestEnvelope
    {
        // To prevent the random delay for retries
        public new DateTimeOffset NextAttemptAt => DateTimeOffset.Now;
    }
}