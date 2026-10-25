using Claims.Auditing;
using Claims.Repositories;
using Claims.Services;
using Claims.Validation;
using NSubstitute;
using Xunit;

namespace Claims.Tests;

public class ClaimsServiceTests
{
    private const string CoverId = "cover-1";
    private static readonly DateTime CoverStart = new(2027, 1, 1);
    private static readonly DateTime CoverEnd = new(2027, 6, 30);

    private readonly IClaimsRepository _claimsRepository = Substitute.For<IClaimsRepository>();
    private readonly ICoversRepository _coversRepository = Substitute.For<ICoversRepository>();
    private readonly IAuditer _auditer = Substitute.For<IAuditer>();
    private readonly ClaimsService _service;

    public ClaimsServiceTests()
    {
        _service = new ClaimsService(_claimsRepository, _coversRepository, _auditer);
    }

    // Makes the fake covers repository return an existing cover for CoverId
    private void SetUpCover()
    {
        var cover = new Cover
        {
            Id = CoverId,
            StartDate = CoverStart,
            EndDate = CoverEnd,
            Type = CoverType.Yacht
        };
        _coversRepository.GetByIdAsync(CoverId).Returns(cover);
    }

    private static Claim NewClaim(DateTime created, decimal damageCost) => new()
    {
        CoverId = CoverId,
        Created = created,
        Name = "Engine fire",
        Type = ClaimType.Fire,
        DamageCost = damageCost
    };

    [Fact]
    public async Task CreateAsync_DamageCostAbove100000_Throws()
    {
        SetUpCover();
        var claim = NewClaim(CoverStart.AddDays(10), 100_001m);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(claim));

        await _claimsRepository.DidNotReceive().AddAsync(Arg.Any<Claim>());
        _auditer.DidNotReceive().AuditClaim(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task CreateAsync_DamageCostOfExactly100000_IsAccepted()
    {
        SetUpCover();
        var claim = NewClaim(CoverStart.AddDays(10), 100_000m);

        var created = await _service.CreateAsync(claim);

        Assert.NotNull(created.Id);
    }

    [Fact]
    public async Task CreateAsync_UnknownCover_Throws()
    {
        // No SetUpCover(): the fake repository returns null for any id
        var claim = NewClaim(CoverStart.AddDays(10), 5_000m);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(claim));
    }

    [Fact]
    public async Task CreateAsync_CreatedBeforeCoverStart_Throws()
    {
        SetUpCover();
        var claim = NewClaim(CoverStart.AddDays(-1), 5_000m);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(claim));
    }

    [Fact]
    public async Task CreateAsync_CreatedAfterCoverEnd_Throws()
    {
        SetUpCover();
        var claim = NewClaim(CoverEnd.AddDays(1), 5_000m);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(claim));
    }

    [Theory]
    [InlineData(0)]   // first day of the cover
    [InlineData(180)] // last day of the cover (1 Jan + 180 days = 30 Jun)
    public async Task CreateAsync_CreatedOnCoverBoundary_IsAccepted(int daysAfterStart)
    {
        SetUpCover();
        var claim = NewClaim(CoverStart.AddDays(daysAfterStart), 5_000m);

        var created = await _service.CreateAsync(claim);

        Assert.NotNull(created.Id);
    }

    [Fact]
    public async Task CreateAsync_ValidClaim_SavesAndAudits()
    {
        SetUpCover();
        var claim = NewClaim(CoverStart.AddDays(10), 5_000m);

        var created = await _service.CreateAsync(claim);

        await _claimsRepository.Received(1).AddAsync(claim);
        _auditer.Received(1).AuditClaim(created.Id, "POST");
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalseAndDoesNotAudit()
    {
        _claimsRepository.DeleteAsync("unknown").Returns(false);

        var deleted = await _service.DeleteAsync("unknown");

        Assert.False(deleted);
        _auditer.DidNotReceive().AuditClaim(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndAudits()
    {
        _claimsRepository.DeleteAsync("abc").Returns(true);

        var deleted = await _service.DeleteAsync("abc");

        Assert.True(deleted);
        _auditer.Received(1).AuditClaim("abc", "DELETE");
    }
}