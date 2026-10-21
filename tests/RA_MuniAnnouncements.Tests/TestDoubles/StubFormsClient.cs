using RA_MuniAnnouncements.Api.ExternalServices;
using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Tests.TestDoubles;

/// <summary>Returns exactly the requests a test supplies; counts calls.</summary>
public sealed class StubFormsClient(params RawFormRequest[] requests) : IFormsSystemClient
{
    public int CallCount { get; private set; }

    public Task<IReadOnlyList<RawFormRequest>> GetRequestsByApplicantIdAsync(string applicantId, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult<IReadOnlyList<RawFormRequest>>(requests);
    }
}
