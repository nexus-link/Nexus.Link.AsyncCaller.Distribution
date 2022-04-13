﻿using System.Collections.Generic;
using Nexus.Link.Libraries.Core.Platform.ServiceMetas;

namespace AsyncCaller.Distribution
{
    public class ReleaseHistory
    {
        public static List<Release> Releases { get; } = new List<Release>
        {
            new Release("1.2.0")
            {
                Notes = new List<Note>
                {
                    Note.Feature("Support for FulcrumResourceLockedException")
                }
            },
            new Release("1.1.5")
            {
                Notes = new List<Note>
                {
                    Note.Fix("Fix for Content-Type in Callback request (it's always application/json, not the content-type from the server response)")
                }
            },
            new Release("1.1.4")
            {
                Notes = new List<Note>
                {
                    Note.Fix("Bugfix for compensation for bug in dotnet core regarding header Expires: -1")
                }
            },
            new Release("1.1.3")
            {
                Notes = new List<Note>
                {
                    Note.Fix("Compensate for bug in dotnet core regarding commas in Server header not allowed")
                }
            },
            new Release("1.1.2")
            {
                Notes = new List<Note>
                {
                    Note.Fix("Setup Correlation Id for logging")
                }
            },
            new Release("1.1.1")
            {
                Notes = new List<Note>
                {
                    Note.Fix("120 s timeout on HttpClient")
                }
            },
            new Release("1.1.0")
            {
                Notes = new List<Note>
                {
                    Note.Feature("Support for multiple tenants")
                }
            },
            new Release("1.0.1")
            {
                Notes = new List<Note>
                {
                    Note.Feature("Log distribution invocations")
                }
            },
            new Release("1.0.0")
            {
                Notes = new List<Note>
                {
                    Note.Feature("Initial release")
                }
            }
        };
    }
}
